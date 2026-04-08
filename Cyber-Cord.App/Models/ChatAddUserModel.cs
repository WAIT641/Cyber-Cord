namespace Cyber_Cord.App.Models;

public class ChatAddUserModel
{
    public int Id { get; set; }
    public string? ChatName { get; set; } = string.Empty;
    public string UserName { get; set; } = default!;
}
