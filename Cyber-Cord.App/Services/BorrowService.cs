using Cyber_Cord.App.Components.Dialogs;

namespace Cyber_Cord.App.Services;

public class BorrowService
{
    private Dictionary<Type, object> _borrowStates = [];

    public BorrowState<TDialog, TModel> GetBorrowState<TDialog, TModel>() where TModel : class where TDialog : ICustomDialog<TModel>
    {
        var type = typeof(BorrowState<TDialog, TModel>);

        if (!_borrowStates.TryGetValue(type, out var value))
        {
            var instance = new BorrowState<TDialog, TModel>();

            _borrowStates.Add(type, instance);

            return instance;
        }

        return (BorrowState<TDialog, TModel>)value;
    }
}
