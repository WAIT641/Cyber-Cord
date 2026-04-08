namespace Cyber_Cord.Api.Entities;

public class UserServer : Entity
{
    public int UserId { get; set; }
    public int ServerId { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Server Server { get; set; } = null!;
}