using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class ChatUpdateDialog : ComponentBase, ICustomDialog<ChatUpdateModel>
{
    public ChatUpdateModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}