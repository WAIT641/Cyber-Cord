using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ColorModel
{
    [JsonRequired]
    public int Red { get; init; }
    [JsonRequired]
    public int Green { get; init; }
    [JsonRequired]
    public int Blue { get; init; }
}
