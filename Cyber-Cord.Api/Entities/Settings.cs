namespace Cyber_Cord.Api.Entities;

public class Settings : Entity
{
    public int UserId { get; set; }
    public bool EnableSounds { get; set; }

    public virtual User User { get; set; } = default!;
}
