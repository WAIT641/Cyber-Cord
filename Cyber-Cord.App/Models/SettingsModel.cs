using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class SettingsModel
{
    [JsonRequired]
    public bool EnableSounds { get; set; }
}
