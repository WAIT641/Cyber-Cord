using Cyber_Cord.App.Extensions;
using Cyber_Cord.App.Models;
using Cyber_Cord.App.Utils;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;

public partial class SignUpSettingsDialog : ComponentBase, ICustomDialog<UserSettingsModel>
{
    private const string _colorPrefix = "#";

    public UserSettingsModel Model { get; set; } = new();

    private string ColorHex {
        get {
            return Model.BannerColor.AsHex(_colorPrefix);
        }
        set {
            if (!ColorUtils.TryGetFromHex(value, out var color, _colorPrefix))
            {
                throw new FormatException("Color value is not in a valid format");
            }

            Model.BannerColor = color;
        }
    }

    public void UpdateState()
    {
        StateHasChanged();
    }
}