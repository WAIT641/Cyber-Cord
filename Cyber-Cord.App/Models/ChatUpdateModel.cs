using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ChatUpdateModel
{
    [JsonIgnore]
    public int ChatId { get; set; }
    [JsonRequired]
    public string? Name { get; set; } = default!;
}