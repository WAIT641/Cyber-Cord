namespace Cyber_Cord.Api.Models;

public class UserReturnModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required ColorReturnModel BannerColor { get; init; }
}
