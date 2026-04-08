using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class UserUpdateModel
{
    [JsonRequired]
    public string? DisplayName { get; set; }
    public string Description { get; set; } = string.Empty;
    [JsonRequired]
    public ColorCreateModel? BannerColor { get; set; }
}
