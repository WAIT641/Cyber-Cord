namespace Cyber_Cord.Api.Models;

public class FriendReturnModel
{
    public required int Id { get; init; }
    public required UserReturnModel OtherUser { get; init; }
}
