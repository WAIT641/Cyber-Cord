using System.Text.Json.Serialization;

namespace Shared.Models.Voice;

public class ConnectResult
{
    [JsonRequired]
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<RemoteParticipantModel>? Participants { get; set; }    
}