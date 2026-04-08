using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ChatAddUserDialog : ComponentBase, ICustomDialog<ChatAddUserModel>
{
    public ChatAddUserModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}