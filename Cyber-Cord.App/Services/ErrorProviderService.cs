using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Services;

public class ErrorProviderService
{
    public bool IsErrorEnabled { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public async Task ShowErrorAsync(EventCallback mainLayoutCallback, string message)
    {
        IsErrorEnabled = true;
        Message = message;

        await mainLayoutCallback.InvokeAsync();
    }

    public async Task HideErrorAsync(EventCallback mainLayoutCallback)
    {
        IsErrorEnabled = false;

        await mainLayoutCallback.InvokeAsync();
    }
}
