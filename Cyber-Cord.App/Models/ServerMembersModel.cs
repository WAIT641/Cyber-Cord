namespace Cyber_Cord.App.Models;

public class ServerMembersModel
{
    public int ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Action<UserModel> ShowUserDialog { get; set; } = default!;
    public Func<UserModel, Task> BanUserAsync { get; set; } = default!;
    public Func<UserModel, Task> KickUserAsync { get; set; } = default!;
    public Func<UserModel, Task> UnbanAsync { get; set; } = default!;
    public List<UserModel> Members = [];
    public List<UserModel> Banned = [];
}
