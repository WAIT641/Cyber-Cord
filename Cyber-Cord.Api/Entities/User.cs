using System.Drawing;
using Microsoft.AspNetCore.Identity;

namespace Cyber_Cord.Api.Entities;

public class User : IdentityUser<int>
{
    public string DisplayName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public virtual Color BannerColor { get; set; }
    public bool IsActivated { get; set; } = false;
    public int? SettingsId { get; set; }
    public string? GoogleId { get; set; }

    public virtual ICollection<UserChat> UserChats { get; set; } = [];
    public virtual ICollection<UserServer> UserServers { get; set; } = [];
    public virtual ICollection<Message> Messages { get; set; } = [];
    public virtual ICollection<Friendship> RequestedFriendships { get; set; } = [];
    public virtual ICollection<Friendship> ReceivedFriendships { get; set; } = [];
    public virtual ICollection<BanUserServer> BanUserServers { get; set; } = [];
    public virtual Settings Settings { get; set; } = default!;
}