using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ErrorModel
{
    [JsonRequired]
    public string Error { get; init; } = default!;
}
