using System.Net.WebSockets;

namespace Cyber_Cord.Api.Runtime;

public class WebSocketSessionData(WebSocket webSocket, CancellationTokenSource cts)
{
    public SemaphoreSlim SemaphoreSlim = new(1, 1);
    public WebSocket WebSocket = webSocket;
    public CancellationTokenSource Cts = cts;
}
