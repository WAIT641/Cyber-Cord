using Cyber_Cord.App.Services;
using Microsoft.AspNetCore.Components;

namespace Cyber_Cord.App.Components.Dialogs;

[CascadingTypeParameter(nameof(TDialog)), CascadingTypeParameter(nameof(TModel))]
public partial class Dialog<TDialog, TModel> : ComponentBase where TDialog : ComponentBase, ICustomDialog<TModel>, new() where TModel : class
{
    [Inject]
    private BorrowService BorrowService { get; set; } = default!;
    private BorrowState<TDialog, TModel> BorrowState => BorrowService.GetBorrowState<TDialog, TModel>();

    private DynamicComponent _innerDialogRef { get; set; } = default!;
    public TDialog InnerDialog { get => (TDialog)_innerDialogRef.Instance!; }

    public TModel Model {
        get => InnerDialog.Model;
        set => InnerDialog.Model = value;
    }
    [Parameter, EditorRequired]
    public EventCallback<Dialog<TDialog, TModel>> OnCancel { get; set; }
    [Parameter, EditorRequired]
    public EventCallback<Dialog<TDialog, TModel>> OnConfirm { get; set; }

    [Parameter]
    public bool ShowConfirmButton { get; set; } = true;
    [Parameter]
    public bool FullHeight { get; set; } = false;
    private string AdditionalContentStyle => FullHeight ? "height: 100vh;" : "";

    private async Task CancelAsync()
    {
        BorrowState.CancelDialog();

        await OnCancel.InvokeAsync(this);
    }

    private async Task ConfirmAsync()
    {
        BorrowState.ConfirmDialog();

        await OnConfirm.InvokeAsync(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
        {
            return;
        }

        Model = BorrowState.Model!;

        StateHasChanged();
        InnerDialog.UpdateState();
    }
}