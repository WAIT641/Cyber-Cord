namespace Cyber_Cord.Api.Entities;

public class Chat : Entity
{
    // Optional name that should only be used for chats that are not 1-to-1
    public string? Name { get; set; }

    public virtual ICollection<UserChat> UserChats { get; set; } = [];
    public virtual ICollection<Message> Messages { get; set; } = [];
}
