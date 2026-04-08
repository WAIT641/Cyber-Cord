using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ChannelUpdateDialog : ComponentBase, ICustomDialog<ChannelUpdateModel>
{
    public ChannelUpdateModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}