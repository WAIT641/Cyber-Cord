using Cyber_Cord.App.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.Models.Voice;

namespace Cyber_Cord.App.Services;

public class CallWindowService(IJSRuntime runtime)
{
    public bool Visible { get; private set; }
    private VoiceTokenModel? _token;
    private EventCallback? _updateStateCallback;

    public void MakeCall(VoiceTokenModel token, EventCallback updateState)
    {
        Visible = true;
        _token = token;
        _updateStateCallback = updateState;
    }

    public async Task<ConnectResult> ConnectAsync(DotNetObjectReference<CallWindow> dotnetRef)
    {
        if (_token is null)
        {
            return new ConnectResult()
            {
                Success = false
            };
        }
        
        var result = await runtime.InvokeAsync<ConnectResult>(
            "livekitInterop.connect", _token.ServerUrl, _token.Token, dotnetRef
            );

        return result;
    }

    public void CloseCall()
    {
        Visible = false;
        _token = null;
        _updateStateCallback?.InvokeAsync();
        _updateStateCallback = null;
    }
}
