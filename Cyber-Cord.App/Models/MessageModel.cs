using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class MessageModel
{
    [JsonRequired]
    public int Id { get; set; }
    public int? UserId { get; set; }
    [JsonRequired]
    public string Content { get; set; } = default!;
    [JsonRequired]
    public DateTime CreatedAt { get; set; }
}