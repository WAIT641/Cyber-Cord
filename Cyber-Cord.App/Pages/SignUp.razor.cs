using System.Drawing;
using Cyber_Cord.App.Components.Dialogs;
using Cyber_Cord.App.Models;
using Cyber_Cord.App.Services;
using Cyber_Cord.App.Utils;
using Microsoft.AspNetCore.Components;
using Shared;

namespace Cyber_Cord.App.Pages;
public partial class SignUp : ComponentBase
{
    private static readonly string _minPasswordLengthValidationText = $"Password must contain at least {GlobalConstants.MinPasswordLength} characters";
    private readonly UserCreateModel _model = new();
    private string _passwordConfirmation = string.Empty;
    private Color Color {
        get
        {
            if (_model.BannerColor is null)
            {
                return Color.Black;
            }

            return Color.FromArgb(_model.BannerColor!.Red!.Value, _model.BannerColor!.Green!.Value, _model.BannerColor!.Blue!.Value);
        }
        set
        {
            _model.BannerColor = new ColorCreateModel
            {
                Red = value.R,
                Green = value.G,
                Blue = value.B,
            };
        }
    }

    [Inject]
    private ApiService ApiService { get; set; } = default!;
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    private SpinnerService SpinnerService { get; set; } = default!;
    [Inject]
    private ErrorProviderService ErrorProviderService { get; set; } = default!;
    [CascadingParameter]
    public EventCallback UpdateState { get; set; }

    private async Task SubmitAsync()
    {
        await SpinnerService.StartLoadingAsync(UpdateState);

        var user = await ApiService.CreateUserAsync(_model);

        if (!user.IsOk())
        {
            await SpinnerService.StopLoadingAsync(UpdateState);
            await ErrorProviderService.ShowErrorAsync(UpdateState, user.Error);
            return;
        }

        await SpinnerService.StopLoadingAsync(UpdateState);
        NavigationManager.NavigateTo($"/validation/{user.Value!.Id}");
    }

    private bool PasswordHasValidLength(string? password)
    {
        if (password is null)
        {
            return false;
        }

        return password.Length >= GlobalConstants.MinPasswordLength;
    }

    private bool PasswordHasNoWhiteSpace(string? password)
    {
        if (password is null)
        {
            return false;
        }

        return !password.Any(char.IsWhiteSpace);
    }

    private bool PasswordHasLowerCase(string? password)
    {
        if (password is null)
        {
            return false;
        }

        return password.Any(char.IsLower);
    }

    private bool PasswordHasUpperCase(string? password)
    {
        if (password is null)
        {
            return false;
        }

        return password.Any(char.IsUpper);
    }

    private bool PasswordHasNumeric(string? password)
    {
        if (password is null)
        {
            return false;
        }

        return password.Any(char.IsNumber);
    }
    
    private bool PasswordHasSpecialChars(string? password)
    {
        if (password is null)
        {
            return false;
        }

        return !password.All(char.IsLetterOrDigit);
    }

    async protected override Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Color = ColorUtils.RandomPrettyColor();
    }

    private void OnOpenDialog()
    {
        var model = new UserSettingsModel()
        {
            Description = _model.Description,
            BannerColor = Color
        };

        BorrowService.GetBorrowState<SignUpSettingsDialog, UserSettingsModel>().ShowDialog(() => model);

        StateHasChanged();
    }

    private void OnConfirmDialog(Dialog<SignUpSettingsDialog, UserSettingsModel>? dialog)
    {
        var model = dialog!.Model;

        Color = model.BannerColor;
        _model.Description = model.Description;

        StateHasChanged();
    }

    private string ShortenText(string? text, int maxCount)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.Length <= maxCount)
        {
            return text;
        }

        return text.Remove(maxCount) + "...";
    }

    private string GetUserInitials() => UserUtils.GetUserInitials(_model.DisplayName ?? "??");

    public async Task UpdateStateAsync() => await InvokeAsync(StateHasChanged);
}