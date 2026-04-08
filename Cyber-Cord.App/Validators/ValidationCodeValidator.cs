using System.Text.RegularExpressions;
using Radzen;
using Radzen.Blazor;

namespace Cyber_Cord.App.Validators;

public class ValidationCodeValidator : ValidatorBase
{
    private string _regexPattern = @"^\d{6}$";

    public override string Text { get; set; } = "Validation code does not have the correct format";

    protected override bool Validate(IRadzenFormComponent component)
    {
        if (!component.HasValue || component.GetValue() is not string value) 
            return false;

        return Regex.IsMatch(value, _regexPattern);
    }
}
