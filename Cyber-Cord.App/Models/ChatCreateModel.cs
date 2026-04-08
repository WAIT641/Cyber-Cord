using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ChatCreateModel
{
    [JsonRequired]
    public string Name { get; set; } = default!;
}