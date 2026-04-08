using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class CursorModel
{
    [JsonRequired]
    public DateTime Time { get; set; }
    [JsonRequired]

    public int Id { get; set; }
}
