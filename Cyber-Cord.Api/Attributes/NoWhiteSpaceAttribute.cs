using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class NoWhiteSpaceAttribute : ValidationAttribute
{
    public override bool IsValid(object? obj)
    {
        if (obj is null || obj is not string value)
        {
            return false;
        } 

        return !value.Any(char.IsWhiteSpace);
    }
}
