namespace Cyber_Cord.Api.Models;

public class FriendRequestDetailModel
{
    public required int Id { get; init; }
    public required UserReturnModel RequestingUser { get; init; }
    public required UserReturnModel ReceivingUser { get; init; }
}
