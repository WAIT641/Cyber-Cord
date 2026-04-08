using Cyber_Cord.Api.Types.Interfaces;

namespace Cyber_Cord.Api.Models;

public class MessageReturnModel : ICursorPaginatable
{
    public required int Id { get; init; }
    public required int? UserId { get; init; }
    public required string Content { get; init; }
    public required DateTime CreatedAt { get; init; }
    public int? ChannelId { get; set; }
    public int? ChatId { get; set; }
}
