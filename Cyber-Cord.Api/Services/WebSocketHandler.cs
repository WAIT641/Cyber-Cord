using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Runtime;
using Cyber_Cord.Api.Types;
using Cyber_Cord.Api.Types.Collections;
using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Shared.Models;
using Shared.Types;
using Shared.Types.Interfaces;

namespace Cyber_Cord.Api.Services;

public class WebSocketHandler(IServiceProvider serviceProvider, IBackgroundJobClient backgroundJobClient, ILogger<IWebSocketHandler> logger) : IWebSocketHandler
{
    private const int _connectionEstablishmentTimeoutMilliseconds = 10_000;
    private const int _defaultBufferSize = 1024;
    private readonly AutoUpdateConcurrentDictionary<int, WebSocketUser, WebSocketSessionData> _webSocketUsers = [];

    public async Task StartSessionAsync(WebSocket webSocket)
    {
        var result = await EstablishSessionAsync(webSocket);

        if (!result.HasValue)
        {
            return;
        }

        var userId = result.Value;

        var cts = new CancellationTokenSource();
        var data = new WebSocketSessionData(webSocket, cts);

        _webSocketUsers.PushOrAdd(userId, data, new());

        try
        {
            await ReceiveLoop(webSocket, cts);
        }
        finally
        {
            _webSocketUsers.PopOrRemove(
                userId,
                data,
                value => value.Dispose()
                );
        }
    }

    public void SendToUser(int userId, IWebSocketMessage message)
    {
        var model = new WebsocketMessageModel
        {
            Messages = [
                message
                ]
        };

        SendToUser(userId, model);
    }

    public void SendToUser(int userId, WebsocketMessageModel message)
    {
        backgroundJobClient.Enqueue(() => SendToUserDirectlyAsync(userId, message));
    }

    public async Task SendToUserDirectlyAsync(int userId, WebsocketMessageModel message)
    {
        if (!_webSocketUsers.TryGetValue(userId, out var webSocketUser))
        {
            return;
        }

        bool kill = message.Messages.Any(
                m => m is WebSocketConfigurationModel c && c.Type == WebSocketConfigurationModel.MessageType.Kill
                );

        using var _ = new AutoReadLock(webSocketUser.Mutex);

        var tasks = new Task[webSocketUser.Sessions.Count];

        foreach (var (index, session) in webSocketUser.Sessions.Index())
        {
            tasks[index] = SendToSessionAsync(session, message, kill);
        }

        await Task.WhenAll(tasks);
    }

    private async Task SendToSessionAsync(WebSocketSessionData data, WebsocketMessageModel message, bool kill)
    {
        var cts = data.Cts;
        var webSocket = data.WebSocket;

        if (cts.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await data.SemaphoreSlim.WaitAsync();

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.ASCII.GetBytes(json);

            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes, 0, bytes.Length),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
                );

            if (kill)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "User was deleted",
                    cts.Token
                    );

                await cts.CancelAsync();
            }
        }
        catch (Exception e)
        {
            logger.LogError("WebSocketHandler:SendToSessionAsync ErrorMessage={}", e.Message);
        }
        finally
        {
            data.SemaphoreSlim.Release();
        }
    }

    private async Task<Optional<int>> EstablishSessionAsync(WebSocket webSocket)
    {
        var cts = new CancellationTokenSource();
        var connection = EstablishConnecionAsync(webSocket, cts.Token);

        await Task.WhenAny(connection, Task.Delay(_connectionEstablishmentTimeoutMilliseconds));

        if (!connection.IsCompleted)
        {
            cts.Cancel();
            return Optional<int>.Empty;
        }

        if (!connection.Result.HasValue)
        {
            return Optional<int>.Empty;
        }

        var userId = connection.Result.Value;

        return new Optional<int>(userId);
    }

    private async Task<Optional<int>> EstablishConnecionAsync(WebSocket webSocket, CancellationToken token)
    {
        string? message;
        WebSocketReceiveResult receiveResult;

        try
        {
            (receiveResult, message) = await ReceiveMessage(webSocket, token);
        }
        catch (Exception e) when (e
            is WebSocketException
            or OperationCanceledException
        ) {
            return Optional<int>.Empty;
        }

        if (receiveResult.MessageType != WebSocketMessageType.Text || string.IsNullOrEmpty(message))
        {
            await Close(WebSocketCloseStatus.InvalidMessageType, "The message was not in the form of a Text or is too long");
            return Optional<int>.Empty;
        }

        var result = GetAuthentication(message);

        if (!result.HasValue)
        {
            await Close(WebSocketCloseStatus.InvalidPayloadData, "The message does not have a valid JSON format");
            return Optional<int>.Empty;
        }

        var grantAccess = Authenticate(result.Value!);

        if (!grantAccess)
        {
            await Close(WebSocketCloseStatus.PolicyViolation, "The user could not be authenticated");
            return Optional<int>.Empty;
        }

        return new Optional<int>(result.Value!.UserId);

        async Task Close(WebSocketCloseStatus status, string message)
        {
            await webSocket.CloseAsync(
                status,
                message,
                CancellationToken.None
                );
        }
    }

    private async static Task ReceiveLoop(WebSocket webSocket, CancellationTokenSource cts)
    {
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var (result, _) = await ReceiveMessage(webSocket, cts.Token);

                if (result.MessageType != WebSocketMessageType.Close)
                {
                    continue;
                }

                await webSocket.CloseAsync(
                    result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                    "Websocket closed upon client request",
                    CancellationToken.None
                    );

                break;
            }
        }
        catch (WebSocketException) { }
        finally
        {
            cts.Cancel();
        }
    }

    private async static Task<(WebSocketReceiveResult, string?)> ReceiveMessage(WebSocket webSocket, CancellationToken ct)
    {
        var buffer = new byte[_defaultBufferSize];
        using var ms = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await webSocket.ReceiveAsync(buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return (result, null);
            }

            ms.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return (
            result,
            result.MessageType == WebSocketMessageType.Text
                ? Encoding.ASCII.GetString(ms.ToArray())
                : null
            );
    }

    private static Optional<WebSocketAuthenticationModel> GetAuthentication(string authentication)
    {
        try
        {
            return new Optional<WebSocketAuthenticationModel>(
                JsonSerializer.Deserialize<WebSocketAuthenticationModel>(authentication)
                );
        }
        catch (Exception e) when (e
            is ArgumentNullException
            or JsonException
            or NotSupportedException
        ) {
            return Optional<WebSocketAuthenticationModel>.Empty;
        }
    }

    private bool Authenticate(WebSocketAuthenticationModel model)
    {
        var userId = model.UserId;
        var code = model.Code;

        using var scope = serviceProvider.CreateScope();
        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        var comparison = memoryCache.Get(userId);
        memoryCache.Remove(userId);

        return comparison is string str && str == code;
    }
}
