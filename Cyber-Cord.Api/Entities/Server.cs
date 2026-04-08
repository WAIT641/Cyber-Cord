namespace Cyber_Cord.Api.Entities;

public class Server : Entity
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int OwnerId { get; set; }

    public virtual ICollection<UserServer> UserServers { get; set; } = [];
    public virtual ICollection<Channel> Channels { get; set; } = [];
    public virtual ICollection<BanUserServer> BanUserServers { get; set; } = [];
}