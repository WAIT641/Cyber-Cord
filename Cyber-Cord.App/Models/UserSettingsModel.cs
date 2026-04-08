using System.Drawing;
using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class UserSettingsModel
{
    [JsonRequired]
    public string Description { get; set; } = default!;
    [JsonRequired]
    public Color BannerColor { get; set; }
}
