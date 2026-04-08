using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ChatUsersDialog : ComponentBase, ICustomDialog<ChatUsersModel>
{
    public ChatUsersModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }

    private async Task KickUserAsync(UserModel user)
    {
        await Model.KickUserAsync(user);
    }
}