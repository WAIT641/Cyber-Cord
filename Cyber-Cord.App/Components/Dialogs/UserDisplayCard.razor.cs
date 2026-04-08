using Cyber_Cord.App.Models;
using Cyber_Cord.App.Services;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class UserDisplayCard : ComponentBase, ICustomDialog<UserDisplayCardModel>
{
    [Inject]
    private ApiService ApiService { get; set; } = default!;

    public UserDisplayCardModel Model { get; set; } = new();

    private bool _isFriendsWith = false;

    async protected override Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
        {
            return;
        }
        
        var result = await ApiService.IsFriendWithAsync(Model.User.Id!.Value);

        _isFriendsWith = result.IsOk();

        await InvokeAsync(StateHasChanged);
    }

    public void UpdateState()
    {
        StateHasChanged();
    }

    private async Task PingAsync()
    {
        await Model.PingAsync(Model.User.Id!.Value);
    }
}