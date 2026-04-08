using Cyber_Cord.App.Models;
using Cyber_Cord.App.Services;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Pages;

public partial class Login
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    private SpinnerService SpinnerService { get; set; } = default!;
    [Inject]
    private ErrorProviderService ErrorProviderService { get; set; } = default!;
    [Inject]
    private ApiService ApiService { get; set; } = default!;
    [CascadingParameter]
    public EventCallback UpdateState { get; set; }

    private UserLoginModel _model = new();

    private async Task SubmitAsync()
    {
        await SpinnerService.StartLoadingAsync(UpdateState);

        var result = await ApiService.LoginAsync(_model.Name!, _model.Password!);

        if (!result)
        {
            await SpinnerService.StopLoadingAsync(UpdateState);

            await ErrorProviderService.ShowErrorAsync(UpdateState, $"User could not be authenticated");
        }

        await SpinnerService.StopLoadingAsync(UpdateState);
        NavigationManager.NavigateTo("/");
    }

    private void RedirectToSignUp()
    {
        NavigationManager.NavigateTo("/signup");
    }

    private void RedirectToGoogleLogin()
    {
        NavigationManager.NavigateTo(ApiService.GetGoogleLoginUrl());
    }
}
