using System.Text.Json.Serialization;

namespace Shared.Models;

public class WebSocketCodeModel
{
    [JsonRequired]
    public string Code { get; set; } = default!;
}
