namespace Cyber_Cord.Api.Models;

public class UserDetailModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string Email { get; init; }
    public required bool IsActivated { get; init; }
    public required ColorReturnModel BannerColor { get; init; }
}
