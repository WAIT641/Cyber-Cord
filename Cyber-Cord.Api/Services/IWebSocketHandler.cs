using System.Net.WebSockets;
using Shared.Models;
using Shared.Types.Interfaces;

namespace Cyber_Cord.Api.Services;

public interface IWebSocketHandler
{
    Task StartSessionAsync(WebSocket webSocket);
    void SendToUser(int userId, IWebSocketMessage message);
    void SendToUser(int userId, WebsocketMessageModel message);
    Task SendToUserDirectlyAsync(int userId, WebsocketMessageModel message);
}