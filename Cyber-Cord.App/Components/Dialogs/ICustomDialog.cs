namespace Cyber_Cord.App.Components.Dialogs;

public interface ICustomDialog<TModel>
{
    TModel Model { get; set; }

    void UpdateState();
}
