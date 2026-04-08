using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class UserModel
{
    [JsonRequired]
    public int? Id { get; set; }
    [JsonRequired]
    public string Name { get; set; } = default!;
    [JsonRequired]
    public string DisplayName { get; set; } = default!;
    [JsonRequired]
    public DateTime? CreatedAt { get; set; }
    [JsonRequired]
    public string Description { get; set; } = default!;
    [JsonRequired]
    public ColorModel BannerColor { get; set; } = default!;
}