using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class UserCreateModel
{
    [JsonRequired]
    public string? Name { get; set; }
    [JsonRequired]
    public string? DisplayName { get; set; }
    [JsonRequired]
    public string? Password { get; set; }
    [JsonRequired]
    public string? Email { get; set; }
    public string Description { get; set; } = string.Empty;
    [JsonRequired]
    public ColorCreateModel? BannerColor { get; set; }
}
