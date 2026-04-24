using Cyber_Cord.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Cyber_Cord.App.Components;

public partial class CallWindow : IDisposable
{
    [Parameter, EditorRequired]
    public string? Name { get; set; }
    [Inject]
    private IJSRuntime Js { get; set; } = default!;
    [Inject]
    private CallWindowService CallWindowService { get; set; } = default!;

    private bool _isMinimized;
    private double _x = 80;
    private double _y = 80;
    private double _width = 320;
    private double _height = 420;

    private bool _isConnected;
    private bool _isMuted;
    private bool _isCameraOn;
    private bool _isBusy;
    private string? _errorMessage;
    private DotNetObjectReference<CallWindow>? _dotnetRef;

    private record ParticipantState(string Identity, string DisplayName, bool IsMuted, bool IsSpeaking);
    private readonly Dictionary<string, ParticipantState> _participants = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Js.InvokeVoidAsync("floatingWindow.init", "call-window", "call-titlebar", "resize-handle", DotNetObjectReference.Create(this));
        }
    }

    private async Task ToggleMinimize()
    {
        _isMinimized = !_isMinimized;
        StateHasChanged();

        if (!_isMinimized)
        {
            await Task.Yield();
            await Js.InvokeVoidAsync("floatingWindow.initResize", "call-window", "resize-handle", DotNetObjectReference.Create(this));
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
            _dotnetRef = DotNetObjectReference.Create(this);

            var result = await CallWindowService.ConnectAsync(_dotnetRef);

            if (!result.Success)
            {
                _errorMessage = $"Connection failed: {result.Error}";
                return;
            }

            if (result.Participants is not null)
            {
                foreach (var p in result.Participants)
                {
                    _participants[p.Identity] = new ParticipantState(p.Identity, p.Name, false, false);
                }
            }

            _isConnected = true;
        }
        catch (Exception ex) { _errorMessage = $"Unexpected error: {ex.Message}"; }
        finally { _isBusy = false; StateHasChanged(); }
    }

    private async Task LeaveAsync()
    {
        await Js.InvokeVoidAsync("livekitInterop.disconnect");
        _isConnected = false;
        _isMuted = false;
        _isCameraOn = false;
        _participants.Clear();
        StateHasChanged();
    }

    private async Task ToggleMuteAsync()
    {
        _isMuted = !_isMuted;
        await Js.InvokeVoidAsync("livekitInterop.setMuted", _isMuted);
        StateHasChanged();
    }

    private async Task ToggleCameraAsync()
    {
        _isCameraOn = !_isCameraOn;
        await Js.InvokeVoidAsync("livekitInterop.setCameraEnabled", _isCameraOn);
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

    public void Dispose()
    {
        _dotnetRef?.Dispose();
    }
}