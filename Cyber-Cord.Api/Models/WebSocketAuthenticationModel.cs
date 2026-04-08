using System.Text.Json.Serialization;

namespace Cyber_Cord.Api.Models;

public class WebSocketAuthenticationModel
{
    [JsonRequired]
    public int UserId { get; set; }
    [JsonRequired]
    public string Code { get; set; } = default!;
}
