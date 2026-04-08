using System.Text.Json.Serialization;
using Shared.Types.Interfaces;

namespace Shared.Models;

// User has been pinged
public class WebSocketPingModel : IWebSocketMessage
{
    [JsonRequired]
    public string OriginatingUserName { get; set; } = default!;
}
