namespace Cyber_Cord.Api.Entities;

public class Friendship : Entity
{
    public int RequestingId { get; set; }
    public int ReceivingId { get; set; }
    public bool Pending { get; set; } = true;

    public virtual User RequestingUser { get; set; } = default!;
    public virtual User ReceivingUser { get; set; } = default!;
}
