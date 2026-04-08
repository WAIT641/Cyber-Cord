using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class PasswordConfirmationDialog : ComponentBase, ICustomDialog<string>
{
    public string Model { get; set; } = string.Empty;

    public void UpdateState()
    {
        StateHasChanged();
    }
}