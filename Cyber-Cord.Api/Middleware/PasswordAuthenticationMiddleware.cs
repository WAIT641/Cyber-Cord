using System.Security.Claims;
using Shared;

namespace Cyber_Cord.Api.Middleware;

public class PasswordAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(Headers.UserPasswordHeader, out var passwordValue))
        {
            await next(context);
            return;
        }

        string? password = passwordValue;

        if (password is null)
        {
            await next(context);
            return;
        }

        var claim = new Claim(Headers.UserPasswordHeader, password);
        var identity = new ClaimsIdentity([claim]);

        context.User.AddIdentity(identity);

        await next(context);
    }
}
