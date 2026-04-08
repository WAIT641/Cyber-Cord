using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ChannelCreateDialog : ComponentBase, ICustomDialog<ChannelCreateModel>
{
    public ChannelCreateModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}