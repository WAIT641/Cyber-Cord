namespace Cyber_Cord.App.Components.Dialogs;

public class BorrowState<TDialog, TModel> where TModel : class
{
    public bool ShowingConfigureDialog => Model is not null;

    public TModel? Model { get; set; }

    public bool Confirmed { get; set; }

    public void ShowDialog(Func<TModel> func)
    {
        Confirmed = false;

        Model = func();
    }

    public void CancelDialog()
    {
        Confirmed = false;

        Model = null;
    }

    public void ConfirmDialog()
    {
        Confirmed = true;

        Model = null;
    }
}
