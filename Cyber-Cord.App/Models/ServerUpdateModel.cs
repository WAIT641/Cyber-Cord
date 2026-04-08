using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ServerUpdateModel
{
    [JsonIgnore]
    public int ServerId { get; set; }
    [JsonRequired]
    public string? Name { get; set; }
    public string? Description { get; set; }
}