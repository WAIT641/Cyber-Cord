using System.Net.WebSockets;
using Shared.Types;
using Cyber_Cord.App.Exceptions;
using Cyber_Cord.App.Services;

namespace Cyber_Cord.App.Shared;

public class SessionState(WebSocketService wsService, IServiceProvider serviceProvider)
{
    public int? UserId { get; private set; }
    public bool IsAuthenticated => UserId is not null;

    public async Task<Result> RequestSessionAsync(int id)
    {
        UserId = null;

        using var scope = serviceProvider.CreateScope();
        var apiService = scope.ServiceProvider.GetRequiredService<ApiService>();

        var code = await apiService.GetWebSocketCodeAsync();

        if (code is null)
        {
            return Result.Bad($"Could not get authentication code for websocket connection");
        }

        try
        {
            await wsService.ConnectAsync(id, code);
        }
        catch (Exception e) when (e
            is OperationCanceledException
            or WebSocketException
            or WebSocketServiceException
        ) {
            return Result.Bad($"Connection error {e.GetType().Name} — {e.Message}");
        }

        UserId = id;
        return Result.Ok();
    }

    public void CloseSession()
    {
        UserId = null;
        wsService.Disconnect();
    }
}
