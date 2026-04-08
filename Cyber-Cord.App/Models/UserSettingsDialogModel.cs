namespace Cyber_Cord.App.Models;

public class UserSettingsDialogModel
{
    public UserDetailModel UserDetail { get; set; } = new();
    public Func<Task> LogOutAsync { get; set; } = default!;
    public Func<Task> GetLatencyAsync { get; set; } = default!;
    public Action DeleteAccount { get; set; } = default!;
    public Action ChangePassword { get; set; } = default!;
}
