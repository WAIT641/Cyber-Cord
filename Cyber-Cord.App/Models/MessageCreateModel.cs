using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class MessageCreateModel
{
    [JsonRequired]
    public required string Content { get; init; }
}
