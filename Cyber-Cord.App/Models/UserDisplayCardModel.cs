namespace Cyber_Cord.App.Models;

public class UserDisplayCardModel
{
    public UserModel User { get; set; } = new();
    public Func<int, Task> PingAsync { get; set; } = default!;
}
