using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ServerMembersDialog : ComponentBase, ICustomDialog<ServerMembersModel>
{
    public ServerMembersModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }

    private async Task BanUserAsync(UserModel user)
    {
        await Model.BanUserAsync(user);
    }

    private async Task KickUserAsync(UserModel user)
    {
        await Model.KickUserAsync(user);
    }

    private async Task UnbanUserAsync(UserModel user)
    {
        await Model.UnbanAsync(user);
    }
}