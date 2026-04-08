using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class FriendRequestModel
{
    [JsonRequired]
    public int Id { get; init; }
    [JsonRequired]
    public UserModel RequestingUser { get; init; } = default!;
    [JsonRequired]
    public UserModel ReceivingUser { get; init; } = default!;
}