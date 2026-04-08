using Microsoft.IdentityModel.JsonWebTokens;

namespace Cyber_Cord.Api.Services;

public class CurrentUserContext(IHttpContextAccessor context) : ICurrentUserContext
{
    private int? _id;
    public string? _name;

    public int GetId()
    {
        if (_id.HasValue)
            return _id.Value;

        var userIdClaim = GetClaim(JwtRegisteredClaimNames.Sub);

        _id = int.Parse(userIdClaim);
        return _id.Value;
    }

    public string GetName()
    {
        if (_name is not null)
            return _name;

        var userNameClaim = GetClaim(JwtRegisteredClaimNames.Name);

        return userNameClaim;
    }

    private string GetClaim(string name)
    {
        var claim = context.HttpContext?.User
            .FindFirst(name)?.Value;

        if (string.IsNullOrEmpty(claim))
            throw new UnauthorizedAccessException($"Unable to access {name} claim");

        return claim;
    }
}
