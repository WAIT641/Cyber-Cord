using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class UserShortModel
{
    [JsonRequired]
    public int Id { get; set; }
    [JsonRequired]
    public string Name { get; set; } = default!;
    [JsonRequired]
    public string DisplayName { get; set; } = default!;
}
