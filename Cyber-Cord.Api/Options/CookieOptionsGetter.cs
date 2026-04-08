namespace Cyber_Cord.Api.Options;

public static class CookieOptionsGetter
{
    public static CookieOptions Get()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = true,
            IsEssential = true,
        };
    }
}
