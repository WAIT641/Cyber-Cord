using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ChatModel
{
    [JsonRequired]
    public int Id { get; set; }
    [JsonRequired]
    public string? Name { get; set; }
}