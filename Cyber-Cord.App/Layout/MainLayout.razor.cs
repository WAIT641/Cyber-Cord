using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Layout;
public partial class MainLayout
{
    private EventCallback UpdateState => EventCallback.Factory.Create(this, UpdateStateAsync);

    private async Task UpdateStateAsync()
    {
        await InvokeAsync(StateHasChanged);
    }

    private async Task HideErrorAsync()
    {
        await ErrorProviderService.HideErrorAsync(UpdateState);
    }
}