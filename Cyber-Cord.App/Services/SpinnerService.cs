using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Services;

public class SpinnerService
{
    public bool IsBusy { get; private set; }

    public async Task StartLoadingAsync(EventCallback mainLayoutCallback)
    {
        IsBusy = true;
        await mainLayoutCallback.InvokeAsync();
    }

    public async Task StopLoadingAsync(EventCallback mainLayoutCallback)
    {
        IsBusy = false;
        await mainLayoutCallback.InvokeAsync();
    }
}
