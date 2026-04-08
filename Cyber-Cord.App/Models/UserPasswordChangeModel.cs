namespace Cyber_Cord.App.Models;

public class UserPasswordChangeModel
{
    public required string OldPassword { get; set; }
    public required string NewPassword { get; set; }
}
