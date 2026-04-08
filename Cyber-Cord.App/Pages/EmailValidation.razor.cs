using Cyber_Cord.App.Services;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Pages;

public partial class EmailValidation : ComponentBase
{
    private string _validationCode = string.Empty;

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

    [Parameter]
    public int UserId { get; set; }
    private string? _password;
    private bool _showPasswordField = false;

    private async Task SubmitAsync()
    {
        await SpinnerService.StartLoadingAsync(UpdateState);

        var result = await ApiService.ActivateUserAsync(UserId, _validationCode);

        if (!result)
        {
            await SpinnerService.StopLoadingAsync(UpdateState);
            await ErrorProviderService.ShowErrorAsync(
                UpdateState,
                "The account could not be activated\nPlease make sure the entered validation code is recent enough"
                );
            return;
        }

        await SpinnerService.StopLoadingAsync(UpdateState);
        NavigationManager.NavigateTo("/login");
    }

    private async Task ResendCodeAsync()
    {
        if (_password is null)
        {
            _showPasswordField = true;
            return;
        }

        await ApiService.ResendValidationCodeAsync(UserId, _password);
    }
}