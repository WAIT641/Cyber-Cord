using System.Drawing;
using Cyber_Cord.App.Models;
using Microsoft.AspNetCore.Components;
using Cyber_Cord.App.Extensions;
using Cyber_Cord.App.Services;
using Cyber_Cord.App.Utils;

namespace Cyber_Cord.App.Components;

public partial class Avatar
{
    private const int _colorAverage = 128;

    [Parameter, EditorRequired]
    public UserModel User { get; set; } = new()
    {
        Name = string.Empty,
        DisplayName = string.Empty,
        Description = string.Empty,
        BannerColor = new(),
    };

    [Parameter]
    public required EventCallback<UserModel> ShowDialog { get; set; }
    [Parameter]
    public bool Clickable { get; set; } = true;

    [Inject]
    public BorrowService BorrowService { get; set; } = default!;

    private string GetUserInitials() => UserUtils.GetUserInitials(User.DisplayName ?? string.Empty);

    private string GetUserBackColor()
    {
        if (User.BannerColor is null)
        {
            return Color.Black.AsHex("#");
        }

        var color = Color.FromArgb(
            User.BannerColor.Red,
            User.BannerColor.Green,
            User.BannerColor.Blue
            );

        return color.AsHex("#");
    }

    private string GetUserFrontColor()
    {
        if (User.BannerColor is null)
        {
            return Color.Black.AsHex("#");
        }

        var average = (User.BannerColor.Red + User.BannerColor.Green + User.BannerColor.Blue) / 3;

        return average > _colorAverage
            ? Color.Black.AsHex("#")
            : Color.White.AsHex("#");
    }

    private async Task OnClickAsync()
    {
        if (Clickable)
        {
            await ShowDialog.InvokeAsync(User);
        }
    }
}