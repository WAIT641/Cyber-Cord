namespace Cyber_Cord.Api.Entities;

public class Channel : Entity
{
    public int ServerId { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;

    public virtual Server Server { get; set; } = null!;
    public virtual ICollection<Message> Messages { get; set; } = [];
}