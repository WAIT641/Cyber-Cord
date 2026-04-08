namespace Cyber_Cord.Api.Entities;

public class UserChat : Entity
{
    public int UserId { get; set; }
    public int ChatId { get; set; }

    public virtual User User { get; set; } = default!;
    public virtual Chat Chat { get; set; } = default!;
}
