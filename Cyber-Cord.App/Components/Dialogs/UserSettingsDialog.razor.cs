using System.Drawing;
using Cyber_Cord.App.Extensions;
using Cyber_Cord.App.Models;
using Cyber_Cord.App.Shared;
using Cyber_Cord.App.Utils;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;
public partial class UserSettingsDialog : ComponentBase, ICustomDialog<UserSettingsDialogModel>
{
    private const string _colorPrefix = "#";
    private const string _defaultColor = "#000000";

    [Inject]
    UserSettingsService UserSettingsService { get; set; } = default!;

    public UserSettingsDialogModel Model { get; set; } = new();

    private string ColorHex {
        get {
            if (Model.UserDetail.BannerColor is null)
            {
                return _defaultColor;
            }

            var color = Color.FromArgb(
                Model.UserDetail.BannerColor.Red,
                Model.UserDetail.BannerColor.Green,
                Model.UserDetail.BannerColor.Blue
                );

            return color.AsHex(_colorPrefix);
        }
        set {
            if (!ColorUtils.TryGetFromHex(value, out var color, _colorPrefix))
            {
                throw new FormatException("Color value is not in a valid format");
            }

            Model.UserDetail.BannerColor = new ColorModel
            {
                Red = color.R,
                Green = color.G,
                Blue = color.B,
            };
        }
    }

    private bool EnableSounds {
        get {
            if (UserSettingsService is null) return false;

            return UserSettingsService.EnableSoundsAsync().Result;
        }
    }

    private async Task InvertSoundsEnabledAsync()
    {
        await UserSettingsService.SetEnableSoundsAsync(!EnableSounds);

        StateHasChanged();
    }

    public void UpdateState()
    {
        StateHasChanged();
    }
}