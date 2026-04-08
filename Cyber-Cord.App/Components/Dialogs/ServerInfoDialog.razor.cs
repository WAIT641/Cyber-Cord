using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ServerInfoDialog : ComponentBase, ICustomDialog<ServerModel>
{
    public ServerModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}