using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class UserChatCreateModel
{
    [JsonRequired]
    public int? UserId { get; set; }
}