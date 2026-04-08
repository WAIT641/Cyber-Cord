using System.Text.Json;
using Cyber_Cord.Api.Constants;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Options;
using Cyber_Cord.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace Cyber_Cord.Api.Controllers;

public class AuthController(IAuthService authService) : BaseAuthorizationController
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var token = await authService.LoginAsync(model);

        Response.Cookies.Append(CookieConstants.JwtName, token, CookieOptionsGetter.Get());

        return Ok();
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieConstants.JwtName);

        return NoContent();
    }

    [HttpPost("ws-code")]
    [Authorize(Roles = RoleNames.User)]
    public IActionResult GetWebSocketCode()
    {
        var code = authService.GetWebSocketCode();

        var json = JsonSerializer.Serialize(new WebSocketCodeModel {
            Code = code,
        });

        return Ok(json);
    }

    [HttpGet("google-login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var url = authService.GetGoogleLoginUrl();

        return Redirect(url);
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
    {
        var tokenModel = await authService.HandleGoogleCallbackAsync(code, state);

        var frontendUrl = authService.GetFrontendUrl();

        Response.Cookies.Append(CookieConstants.JwtName, tokenModel.Token, CookieOptionsGetter.Get());

        return Redirect($"{frontendUrl}");
    }
}