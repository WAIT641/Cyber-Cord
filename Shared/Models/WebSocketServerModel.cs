using System.Text.Json.Serialization;
using Shared.Types.Interfaces;

namespace Shared.Models;

// Server changes (new channel, new user in server...)
public class WebSocketServerModel : IWebSocketMessage
{
    public enum ScopeType
    {
        Channel,
        User,
        Update
    }

    [JsonRequired]
    public ScopeType Scope { get; set; }
    [JsonRequired]
    public int ServerId { get; set; }
}
