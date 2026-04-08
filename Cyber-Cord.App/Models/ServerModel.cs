using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ServerModel
{
    [JsonRequired]
    public int Id { get; set; }
    [JsonRequired]
    public string Name { get; set; } = default!;
    [JsonRequired]
    public string Description { get; set; } = default!;
    [JsonRequired]
    public int OwnerId { get; set; }
}