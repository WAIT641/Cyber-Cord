using Microsoft.AspNetCore.Components;
using Cyber_Cord.App.Models;

namespace Cyber_Cord.App.Components.Dialogs;

public partial class ChangePasswordDialog : ComponentBase, ICustomDialog<UserPasswordChangeModel>
{
    public UserPasswordChangeModel Model { get; set; } = new()
    {
        NewPassword = string.Empty,
        OldPassword = string.Empty,
    };

    public void UpdateState()
    {
        StateHasChanged();
    }
}