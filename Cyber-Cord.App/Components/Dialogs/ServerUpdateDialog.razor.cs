using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ServerUpdateDialog : ComponentBase, ICustomDialog<ServerUpdateModel>
{
    public ServerUpdateModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}