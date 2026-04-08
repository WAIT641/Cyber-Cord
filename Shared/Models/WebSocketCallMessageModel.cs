using System.Text.Json.Serialization;
using Shared.Types.Interfaces;

namespace Shared.Models;

public class WebSocketCallMessageModel : IWebSocketMessage
{
    public enum MessageType
    {
        Error,
        CallOffer,
        CallAnswer,
        CallIceCandidate,
        CallEnded,
        CallRejected
    }
    
    [JsonRequired]
    public MessageType Type { get; set; }
    [JsonRequired]
    public int CallStarterId { get; set; }
    [JsonRequired]
    public string Sdp { get; set; }
}