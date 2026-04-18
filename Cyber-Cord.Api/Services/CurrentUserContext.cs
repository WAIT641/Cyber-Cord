using System.Reflection.PortableExecutable;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Cyber_Cord.Api.Services;

public class CurrentUserContext(IHttpContextAccessor context) : ICurrentUserContext
{
    private int? _id;
    private string? _name;
    private string? _displayName; 

    public int GetId() => GetClaim(
        JwtRegisteredClaimNames.Sub,
        () => _id,
        (str) => _id = int.Parse(str)
    );

    public string GetName() => GetClaim(
        JwtRegisteredClaimNames.Name,
        () => _name,
        (str) => _name = str
    );

    public string GetDisplayName() => GetClaim(
        JwtRegisteredClaimNames.PreferredUsername,
        () => _displayName,
        (str) => _displayName = str
    );

    private T GetClaim<T>(string name, Func<T?> getter, Action<string> setter) where T : struct
    {
        if (getter() is not null)
        {
            return (T)getter()!;
        }
        
        LoadClaim(name, setter);
        
        return (T)getter()!;
    }
    
    private T GetClaim<T>(string name, Func<T?> getter, Action<string> setter) where T : class
    {
        if (getter() is not null)
        {
            return getter()!;
        }
        
        LoadClaim(name, setter);
        
        return getter()!;
    }

    private void LoadClaim(string name, Action<string> setter)
    {
        var claim = context.HttpContext?.User
            .FindFirst(name)?.Value;

        if (string.IsNullOrEmpty(claim))
            throw new UnauthorizedAccessException($"Unable to access {name} claim");
        
        setter(claim);
    }
}
