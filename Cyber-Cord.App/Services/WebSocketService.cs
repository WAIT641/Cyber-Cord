using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Cyber_Cord.App.Exceptions;
using Cyber_Cord.App.Options;
using Microsoft.Extensions.Options;
using Shared.Models;

namespace Cyber_Cord.App.Services;

public class WebSocketService(IOptions<RouteOptions> options) : IDisposable
{
    private ClientWebSocket? _client;
    private CancellationTokenSource _cts = new();
    private const int _bufferSize = 4096;

    public event Func<WebsocketMessageModel, Task> ReceivedMessageAsync = default!;

    public void Disconnect() => _cts.Cancel();

    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="WebSocketException"></exception>
    /// <exception cref="WebSocketServiceException"></exception>
    public async Task ConnectAsync(int userId, string code)
    {
        if (_client is not null)
        {
            _client.Dispose();
        }

        await InitializeAsync(userId, code);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_client is not null)
        {
            if (disposing)
            {
                _client.Dispose();
            }
            
            _client = null;
        }
    }

    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="WebSocketException"></exception>
    /// <exception cref="WebSocketServiceException"></exception>
    private async Task InitializeAsync(int userId, string code)
    {
        if (options.Value.WebSocketRoute is null)
        {
            throw new WebSocketServiceException("Web socket endpoint was not configured for this app instance");
        }

        var webSocket = new ClientWebSocket();
        
        try
        {
            await webSocket.ConnectAsync(
                new Uri(options.Value.WebSocketRoute!),
                CancellationToken.None
                );

            var json = JsonSerializer.Serialize(new {
                UserId = userId,
                Code = code
                });

            await webSocket.SendAsync(
                Encoding.ASCII.GetBytes(json),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
                );
        }
        catch
        {
            if (webSocket is not null)
            {
                webSocket.Dispose();
            }

            throw;
        }

        _client = webSocket;

        _ = Task.Run(ReceiveLoopAsync);
    }

    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="WebSocketException"></exception>
    private async Task ReceiveLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            var (result, message) = await ReceiveMessage(_cts.Token, _client!);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;  
            }

            if (string.IsNullOrEmpty(message))
            {
                await ReceivedMessageAsync(ErrorMessage("Incoming websocket message was emtpy or null"));
                continue;
            }

            try
            {
                var model = JsonSerializer.Deserialize<WebsocketMessageModel>(message);

                if (model is not null)
                {
                    await ReceivedMessageAsync(model);
                    continue;
                }
            }
            catch (Exception e) when (e
                is ArgumentNullException
                or JsonException
                or NotSupportedException
            ) { }

            await ReceivedMessageAsync(ErrorMessage("Incoming websocket message could not be parsed"));
        }

        await CloseWebSocketAsync();
    }

    private async Task CloseWebSocketAsync()
    {
        await _client!.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "The client closed the connection",
            CancellationToken.None
            );
    }

    private async Task<(WebSocketReceiveResult, string?)> ReceiveMessage(CancellationToken ct, WebSocket webSocket)
    {
        var buffer = new byte[_bufferSize];
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

        return (result, Encoding.ASCII.GetString(ms.ToArray()));
    }

    ~WebSocketService()
    {
        Dispose(false);
    }

    private WebsocketMessageModel ErrorMessage(string error)
    {
        return new()
        {
            Messages = [
                new WebSocketConfigurationModel
                {
                    Type = WebSocketConfigurationModel.MessageType.Error,
                    Reason = error
                }
            ]
        };
    }
}
