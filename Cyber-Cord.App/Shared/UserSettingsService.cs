using Cyber_Cord.App.Models;
using Cyber_Cord.App.Services;

namespace Cyber_Cord.App.Shared;

public class UserSettingsService(IServiceProvider serviceProvider)
{
    private SettingsModel? _settings;

    public async Task<SettingsModel?> GetAsync()
    {
        if (_settings is not null)
        {
            return _settings;
        }

        var scope = serviceProvider.CreateScope();
        var apiService = scope.ServiceProvider.GetRequiredService<ApiService>();

        _settings = await apiService.GetUserSettingsAsync();

        return _settings;
    }

    public async Task ReloadAsync()
    {
        _settings = null;
        _ = await GetAsync();
    }

    public async Task<bool> EnableSoundsAsync() => await GetGeneralAsync(s => s.EnableSounds);

    /// <summary>
    /// Return whether setting the value was successful
    /// </summary>
    public async Task<bool> SetEnableSoundsAsync(bool enable) => await SetGeneralAsync(s => s.EnableSounds, enable);

    private async Task<T> GetGeneralAsync<T>(Func<SettingsModel, T> selector)
    {
        var settings = await GetAsync();

        if (settings is null)
        {
            return default!;
        }

        return selector(settings);
    }
    
    private async Task<bool> SetGeneralAsync<T>(System.Linq.Expressions.Expression<Func<SettingsModel, T>> selector, T value)
    {
        var scope = serviceProvider.CreateScope();
        var apiService = scope.ServiceProvider.GetRequiredService<ApiService>();

        var result = await apiService.ReplacePatchSettingsAsync(selector, value);

        await ReloadAsync();

        return result;
    }
}
