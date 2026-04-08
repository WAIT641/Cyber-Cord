using Cyber_Cord.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cyber_Cord.Api.Services;

public interface IAuthService
{
    Task<UserReturnModel> ExtractUserInformationFromCookie(string cookie);
    string GetFrontendUrl();
    string GetGoogleLoginUrl();
    string GetWebSocketCode();
    Task<TokenModel> HandleGoogleCallbackAsync(string code, string state);
    Task<string> LoginAsync([FromBody] LoginModel model);
}