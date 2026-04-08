using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ServerCreateModel
{
    [JsonRequired]
    public string? Name { get; set; }
    public string? Description { get; set; }
}