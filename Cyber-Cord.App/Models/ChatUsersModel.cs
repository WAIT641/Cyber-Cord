namespace Cyber_Cord.App.Models;

public class ChatUsersModel
{
    public int ChatId { get; set; }
    public string? Name { get; set; } = string.Empty;
    public Action<UserModel> ShowUserDialog { get; set; } = default!;
    public Func<UserModel, Task> KickUserAsync { get; set; } = default!;
    public List<UserModel> Users = [];
}
