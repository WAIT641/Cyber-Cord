using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ChannelUpdateModel
{
    [JsonIgnore]
    public int ChannelId { get; set; }
    [JsonRequired]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}