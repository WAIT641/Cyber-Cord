using Cyber_Cord.App.Pages;
using Cyber_Cord.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Cyber_Cord.App.Components;

public partial class CallWindow : IAsyncDisposable
{
    [Parameter, EditorRequired]
    public string? Name { get; set; }
    [Inject]
    private IJSRuntime JS { get; set; } = default!;
    [Inject]
    private CallWindowService CallWindowService { get; set; } = default!;

    private bool _isMinimized = false;
    private double _x = 80;
    private double _y = 80;
    private double _width = 320;
    private double _height = 420;

    private bool _isConnected = false;
    private bool _isMuted = false;
    private bool _isCameraOn = false;
    private bool _isBusy = false;
    private string? _errorMessage;
    private DotNetObjectReference<FloatingCallWindow>? _dotnetRef;

    private record ParticipantState(string Identity, string DisplayName, bool IsMuted, bool IsSpeaking);
    private Dictionary<string, ParticipantState> _participants = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS!.InvokeVoidAsync("floatingWindow.init", "call-window", "call-titlebar", "resize-handle", DotNetObjectReference.Create(this));
        }
    }

    private async Task ToggleMinimize()
    {
        _isMinimized = !_isMinimized;
        StateHasChanged();

        if (!_isMinimized)
        {
            await Task.Delay(50); // TODO Task.Yield
            await JS.InvokeVoidAsync("floatingWindow.initResize", "call-window", "resize-handle", DotNetObjectReference.Create(this));
        }
    }

    private async Task CloseWindow()
    {
        if (_isConnected)
            await LeaveAsync();
        CallWindowService.CloseCall();
    }

    [JSInvokable]
    public void UpdatePosition(double x, double y)
    {
        _x = x;
        _y = y;
        StateHasChanged();
    }

    [JSInvokable]
    public void UpdateSize(double width, double height)
    {
        _width = Math.Max(260, width);
        _height = Math.Max(300, height);
        StateHasChanged();
    }

    private async Task JoinAsync()
    {
        _isBusy = true;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            // TODO REDO

            /*if (!response.IsSuccessStatusCode) { _errorMessage = "Failed to get voice token."; return; }

            var tokenDto = await response.Content.ReadFromJsonAsync<VoiceTokenDto>();
            if (tokenDto is null) { _errorMessage = "Invalid token response."; return; }

            _dotnetRef = DotNetObjectReference.Create(this);
            var result = await JS.InvokeAsync<ConnectResult>(
                "livekitInterop.connect", tokenDto.ServerUrl, tokenDto.Token, _dotnetRef);

            if (!result.Success) { _errorMessage = $"Connection failed: {result.Error}"; return; }

            if (result.Participants is not null)
                foreach (var p in result.Participants)
                    _participants[p.Identity] = new ParticipantState(p.Identity, p.Name, false, false);*/

            _isConnected = true;
        }
        catch (Exception ex) { _errorMessage = $"Unexpected error: {ex.Message}"; }
        finally { _isBusy = false; StateHasChanged(); }
    }

    private async Task LeaveAsync()
    {
        await JS.InvokeVoidAsync("livekitInterop.disconnect");
        _isConnected = false;
        _isMuted = false;
        _isCameraOn = false;
        _participants.Clear();
        StateHasChanged();
    }

    private async Task ToggleMuteAsync()
    {
        _isMuted = !_isMuted;
        await JS.InvokeVoidAsync("livekitInterop.setMuted", _isMuted);
        StateHasChanged();
    }

    private async Task ToggleCameraAsync()
    {
        _isCameraOn = !_isCameraOn;
        await JS.InvokeVoidAsync("livekitInterop.setCameraEnabled", _isCameraOn);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnParticipantJoined(string identity, string displayName)
    {
        _participants[identity] = new ParticipantState(identity, displayName, false, false);
        InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void OnParticipantLeft(string identity)
    {
        _participants.Remove(identity);
        InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void OnActiveSpeakersChanged(List<string> speakerIdentities)
    {
        var set = speakerIdentities.ToHashSet();
        foreach (var key in _participants.Keys.ToList())
            _participants[key] = _participants[key] with { IsSpeaking = set.Contains(key) };
        InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void OnParticipantMuteChanged(string identity, bool isMuted)
    {
        if (_participants.TryGetValue(identity, out var p))
            _participants[identity] = p with { IsMuted = isMuted };
        InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void OnDisconnected()
    {
        _isConnected = false;
        _participants.Clear();
        InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isConnected)
            await JS.InvokeVoidAsync("livekitInterop.disconnect");
        _dotnetRef?.Dispose();
    }
}