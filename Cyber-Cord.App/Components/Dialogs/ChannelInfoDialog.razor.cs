using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ChannelInfoDialog : ComponentBase, ICustomDialog<ChannelModel>
{
    public ChannelModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}