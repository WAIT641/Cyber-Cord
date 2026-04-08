using Microsoft.JSInterop;
using Cyber_Cord.App.Enums;
using Cyber_Cord.App.Shared;

namespace Cyber_Cord.App.Services;

public class SoundNotificationService(IJSRuntime jsRuntime, UserSettingsService settings)
{
    public async Task PlayNotificationAsync(NotificationSound sound)
    {
        if (!await settings.EnableSoundsAsync())
        {
            return;
        }

        var suppoerted = await IsSoundSupportedAsync();
        if (!suppoerted)
            return;
        
        var soundPath = GetSoundPath(sound);
        await jsRuntime.InvokeVoidAsync("playNotificationSound", soundPath);
    }
    
    public async Task<bool> IsSoundSupportedAsync()
    {
        return await jsRuntime.InvokeAsync<bool>("isSoundSupported");
    }

    private string GetSoundPath(NotificationSound sound)
    {
        return sound switch
        {
            NotificationSound.MessageReceived => "/sounds/Notification.wav",
            NotificationSound.FriendRequest => "/sounds/Notification.wav",
            //NotificationSound.Mention => "/sounds/mention.mp3",
            NotificationSound.Join => "/sounds/Join.wav",
            NotificationSound.Leave => "/sounds/Leave.wav",
            _ => "/sounds/Notification.wav"
        };
    }
}