using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ChannelCreateModel
{
    [JsonRequired]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}