using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class MessageEditDialog : ComponentBase, ICustomDialog<MessageUpdateModel>
{
    public MessageUpdateModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}