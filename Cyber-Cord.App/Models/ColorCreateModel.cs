using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class ColorCreateModel
{
    [JsonRequired]
    public int? Red { get; set; }
    [JsonRequired]
    public int? Green { get; set; }
    [JsonRequired]
    public int? Blue { get; set; }
}
