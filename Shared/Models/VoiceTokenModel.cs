using System.Text.Json.Serialization;

namespace Shared.Models;

public class VoiceTokenModel
{
    [JsonRequired]
    public string? Token { get; set; }
    [JsonRequired]
    public string? RoomId { get; set; }
    [JsonRequired]
    public string? ServerUrl { get; set; }
}