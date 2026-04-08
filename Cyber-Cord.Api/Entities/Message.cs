namespace Cyber_Cord.Api.Entities;

public class Message : Entity
{
    public int? ChatId { get; set; }
    public int? ChannelId { get; set; }
    public int? UserId { get; set; }
    public string Content { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public virtual Chat? Chat { get; set; }
    public virtual Channel? Channel { get; set; }
    public virtual User User { get; set; } = default!;
}