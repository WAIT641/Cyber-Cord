using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ServerCreateDialog : ComponentBase, ICustomDialog<ServerCreateModel>
{
    public ServerCreateModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}