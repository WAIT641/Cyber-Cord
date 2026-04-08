using System.Text.Json.Serialization;
using Shared.Types.Interfaces;

namespace Shared.Models;

public class WebsocketMessageModel
{
    // List is used because certain endpoints may cause multiple changes (accepting a friendship adds a new friend and a new chat)
    // Could also be used for future support of buffering websocket messages
    [JsonRequired]
    public List<IWebSocketMessage> Messages { get; set; } = [];
}