using Radzen;
using Radzen.Blazor;

namespace Cyber_Cord.App.Validators;

public class NoWhiteSpaceValidator : ValidatorBase
{
    public override string Text { get; set; } = "Cannot contain white space";

    protected override bool Validate(IRadzenFormComponent component)
    {
        if (!component.HasValue || component.GetValue() is not string value)
            return false;

        return !value.Any(char.IsWhiteSpace);
    }
}
