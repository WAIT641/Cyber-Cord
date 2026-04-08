using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class UserDetailModel
{
    [JsonRequired]
    public int Id { get; init; } = default!;
    [JsonRequired]
    public string Name { get; init; } = default!;
    [JsonRequired]
    public string DisplayName { get; set; } = default!;
    [JsonRequired]
    public string Description { get; set; } = default!;
    [JsonRequired]
    public DateTime CreatedAt { get; init; } = default!;
    [JsonRequired]
    public string Email { get; init; } = default!;
    [JsonRequired]
    public bool? IsActivated { get; init; }
    [JsonRequired]
    public ColorModel BannerColor { get; set; } = default!;
}
