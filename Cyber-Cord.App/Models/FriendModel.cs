using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class FriendModel
{
    [JsonRequired]
    public int Id { get; init; }
    [JsonRequired]
    public UserModel OtherUser { get; init; } = default!;
}