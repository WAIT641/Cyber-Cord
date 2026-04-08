using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class AddServerMemberDialog : ComponentBase, ICustomDialog<MemberFindModel>
{
    public MemberFindModel Model { get; set; } = new();

    public void UpdateState()
    {
        StateHasChanged();
    }
}