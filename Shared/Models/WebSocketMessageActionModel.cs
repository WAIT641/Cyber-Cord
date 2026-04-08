using System.Text.Json.Serialization;
using Shared.Types.Interfaces;

namespace Shared.Models;

// New message (in chat, in channel)
public class WebSocketMessageActionModel : IWebSocketMessage
{
    public enum ActionType
    {
        Received,
        Removed,
        Altered,
    }

    [JsonRequired]
    public ActionType Action { get; set; }
    [JsonRequired]
    public int MessageId { get; set; }
    
    public int? ChatId { get; set; }
    public int? ChannelId { get; set; }
}
