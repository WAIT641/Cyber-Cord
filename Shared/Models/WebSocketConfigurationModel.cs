using System.Text.Json.Serialization;
using Shared.Types.Interfaces;

namespace Shared.Models;

// End connection
public class WebSocketConfigurationModel : IWebSocketMessage
{
    public enum MessageType
    {
        Error,
        Kill,
        Verify
    }

    [JsonRequired]
    public MessageType Type {  get; set; }
    public string? Reason { get; set; }
}
