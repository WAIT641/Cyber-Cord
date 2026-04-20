using System.Text.Json.Serialization;

namespace Shared.Models.Voice;

public class RemoteParticipantModel
{
    [JsonRequired]
    public string Identity { get; set; } = default!;
    [JsonRequired]
    public string Name { get; set; } = default!;
}