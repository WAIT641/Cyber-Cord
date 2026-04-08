using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ChatCreateDialog : ComponentBase, ICustomDialog<ChatCreateModel>
{
    public ChatCreateModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}