using System.Net.Http.Json;
using Cyber_Cord.App.Components.Dialogs;
using Microsoft.JSInterop;
using Shared.Models;

namespace Cyber_Cord.App.Services;

public enum CallState { Idle, Calling, Ringing, Connected }

public class CallHandlerService : IAsyncDisposable
{
    public event Action? OnStateChanged;
    public event Action<string>? OnIncomingCall;
    public event Action? OnCallEnded;

    public CallState State { get; private set; } = CallState.Idle;
    public int? ActivePeerId { get; private set; }
    public int? IncomingChatId { get; private set; }

    private readonly IJSRuntime _js;
    private readonly WebSocketService _ws;

    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<CallHandlerService>? _dotnetRef;

    private string? _pendingOfferSdp;
    
    ApiService _apiService;

    public CallHandlerService(IJSRuntime js, WebSocketService ws, ApiService apiService)
    {
        _js   = js;
        _ws   = ws;
        _apiService = apiService;
    }
    
    public async Task InitialiseAsync()
    {
        _jsModule  = await _js.InvokeAsync<IJSObjectReference>("import", "./js/webrtc.js");
        _dotnetRef = DotNetObjectReference.Create(this);
    }

    // JS creates offer SDP, then calls OnOfferReadyAsync below
    public async Task CallAsync(int targetUserId)
    {
        if (State != CallState.Idle)
            throw new InvalidOperationException("Already in a call.");

        ActivePeerId    = targetUserId;
        SetState(CallState.Calling);

        // JS will call back into OnOfferReadyAsync once the offer is ready
        await _jsModule!.InvokeVoidAsync("startCall", _dotnetRef, targetUserId.ToString(), true);
    }

    // JS fires this once the offer SDP is ready
    [JSInvokable]
    public async Task OnOfferReadyAsync(string targetChatId/*has to be string bcs JS*/, string sdp)
    {
        int.TryParse(targetChatId, out var id);
        await _apiService.StartChatCall(id, sdp);
    }

    // ── Incoming call ─────────────────────────────────────────────────────────

    public async Task AcceptCallAsync(int targetChatId)
    {
        if (State != CallState.Ringing || IncomingChatId is null) return;

        // Tell JS to set up the callee side and produce an answer for the pending offer
        await _jsModule!.InvokeVoidAsync("startCall", _dotnetRef, IncomingChatId.ToString(), false);
        if (_jsModule != null)
        {
            var answerSdp = await _jsModule.InvokeAsync<string>("receiveOffer", _pendingOfferSdp);
            
            // POST the answer to the API — it will push it to the caller via WebSocket
            await _apiService.AcceptCall(targetChatId, answerSdp);
        }

        ActivePeerId     = IncomingChatId;
        IncomingChatId = null;
        _pendingOfferSdp = null;
        SetState(CallState.Connected);
    }

    public async Task RejectCallAsync(int targetChatId)
    {
        if (State != CallState.Ringing || IncomingChatId is null) return;


        await _apiService.RejectChatCall(targetChatId);

        IncomingChatId = null;
        _pendingOfferSdp = null;
        SetState(CallState.Idle);
    }

    // ── End call ──────────────────────────────────────────────────────────────

    public async Task EndCallAsync(int chatId)
    {
        if (State == CallState.Idle) return;

        if (ActivePeerId.HasValue)
        {
            await _apiService.EndCall(chatId);
        }

        await TeardownAsync();
    }

    // ── WebSocket message handler ─────────────────────────────────────────────
    // Handles messages pushed by the API after REST calls complete

    public async Task HandleWebSocketCallMessageAsync(WebSocketCallMessageModel msg)
    {
        switch (msg.Type)
        {
            // Caller receives this when callee accepts via POST /api/calls/respond
            case WebSocketCallMessageModel.MessageType.CallAnswer:
                await HandleAnswerAsync(msg.Sdp);
                break;

            // Caller receives this when callee declines via POST /api/calls/respond
            case WebSocketCallMessageModel.MessageType.CallRejected:
                await TeardownAsync();
                OnCallEnded?.Invoke();
                break;

            // Client B receives this when Client A POSTs /api/calls/start
            case WebSocketCallMessageModel.MessageType.CallOffer:
                await HandleIncomingOffer(msg.CallStarterId, msg.Sdp);
                break;

            // Either party receives this when the other POSTs /api/calls/end
            case WebSocketCallMessageModel.MessageType.CallEnded:
                await TeardownAsync();
                OnCallEnded?.Invoke();
                break;

            // ICE candidates still come directly over WebSocket (high frequency)
            case WebSocketCallMessageModel.MessageType.CallIceCandidate:
                await HandleIceCandidateAsync(msg.Sdp);
                break;

        }
    }

    // ── Internal handlers ─────────────────────────────────────────────────────

    private async Task HandleIncomingOffer(int chatId, string sdp)
    {
        if (State != CallState.Idle)
        {
            await _apiService.RejectChatCall(chatId);
            return;
        }

        IncomingChatId = chatId;
        _pendingOfferSdp = sdp;
        SetState(CallState.Ringing);
        OnIncomingCall?.Invoke(chatId.ToString());
    }

    private async Task HandleAnswerAsync(string sdp)
    {
        await _jsModule!.InvokeVoidAsync("receiveAnswer", sdp);
        SetState(CallState.Connected);
    }

    private async Task HandleIceCandidateAsync(string candidate)
    {
        if (_jsModule is not null)
            await _jsModule.InvokeVoidAsync("addIceCandidate", candidate);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task TeardownAsync()
    {
        ActivePeerId    = null;

        if (_jsModule is not null)
            await _jsModule.InvokeVoidAsync("endCall");

        SetState(CallState.Idle);
    }

    private void SetState(CallState state)
    {
        State = state;
        OnStateChanged?.Invoke();
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        await TeardownAsync();
        _dotnetRef?.Dispose();
        if (_jsModule is not null) await _jsModule.DisposeAsync();
    }
}
