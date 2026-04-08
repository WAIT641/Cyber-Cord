using System.Buffers.Text;
using System.Drawing;
using System.Security.Cryptography;
using System.Text.Json;
using Cyber_Cord.Api.Constants;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Exceptions;
using Cyber_Cord.Api.Extensions;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Google;
using Cyber_Cord.Api.Repositories;
using Cyber_Cord.Api.Types.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Cyber_Cord.Api.Services;

public class AuthService(
    ICustomPasswordHasher hasher,
    UserManager<User> userManager,
    ITokenProvider tokenProvider,
    ICurrentUserContext userContext,
    IMemoryCache memoryCache,
    IConfiguration configuration,
    IUsersRepository usersRepository,
    IHttpClientFactory httpClientFactory,
    IUnitOfWork uow
) : IAuthService {
    private const int _webSocketCodeLength = 64;
    private const int _webSocketCodeMinutes = 1;
    private const string _allowedCodeCharacters = "qwertzuiopasdfghjklyxcvbnmQWERTZUIOPASDFGHJKLYXCVBNM123456789-._~";
    private const int _codeVerifierLength = 128;
    private const int _stateLength = 64;
    private const int _stateValidityMinutes = 5;

    public async Task<string> LoginAsync([FromBody] LoginModel model)
    {
        var user = await userManager.FindByNameAsync(model.UserName);

        if (user is null)
        {
            throw new NotFoundException($"Could not find a user with email {model.UserName}");
        }

        if (!user.IsActivated)
        {
            throw new ForbiddenException("This user was not activated");
        }

        var result = hasher.CheckPassword(model.Password, user.PasswordHash!);

        if (!result)
        {
            throw new UnauthorizedException("Password is incorrect");
        }

        var token = await tokenProvider.CreateAsync(user);

        return token;
    }

    public string GetWebSocketCode()
    {
        var userId = userContext.GetId();

        var code = RandomNumberGenerator.GetHexString(_webSocketCodeLength);

        memoryCache.Set(userId, code, DateTime.UtcNow + TimeSpan.FromMinutes(_webSocketCodeMinutes));

        return code;
    }

    public string GetGoogleLoginUrl()
    {
        var clientId = configuration[OAuthConstants.Client]!;
        var redirectUri = configuration[OAuthConstants.RedirectUri]!;

        var codeVerifier = GenerateCodeVerifier();
        var codeChallange = GenerateCodeChallange(codeVerifier);
        var state = RandomNumberGenerator.GetHexString(_stateLength);

        memoryCache.Set(state, codeVerifier, DateTime.UtcNow + TimeSpan.FromMinutes(_stateValidityMinutes));

        var url = "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&response_type=code" +
            "&scope=openid%20email%20profile" +
            "&access_type=offline" +
            $"&code_challenge={Uri.EscapeDataString(codeChallange)}" +
            "&code_challenge_method=S256" +
            $"&state={Uri.EscapeDataString(state)}";

        return url;
    }

    public async Task<TokenModel> HandleGoogleCallbackAsync(string code, string state)
    {
        if (!memoryCache.TryGetValue(state, out var value) || value is not string codeVerifier)
        {
            throw new BadRequestException("Could not get code_verifier");
        }

        memoryCache.Remove(state);

        var tokenResponse = await ExchangeCodeForTokensAsync(code, codeVerifier, state);
        var userInfo = ExtractUserInfoFromIdToken(tokenResponse.IdToken);
        var user = await FindOrCreateGoogleUserAsync(userInfo);

        var jwt = await tokenProvider.CreateAsync(user);

        return new TokenModel { Token = jwt };
    }

    public async Task<UserReturnModel> ExtractUserInformationFromCookie(string cookie)
    {
        var handler = new JsonWebTokenHandler();
        var token = handler.ReadJsonWebToken(cookie);

        var user = await userManager.FindByIdAsync(token.GetClaim(JwtRegisteredClaimNames.Sub).Value);

        if (user is null)
        {
            throw new NotFoundException("Could not find user with this id");
        }

        return user.ToReturnModel();
    }

    public string GetFrontendUrl() => configuration[OAuthConstants.FrontendUrl]!;

    // === End of Public API ===

    private async Task<GoogleTokenResponse> ExchangeCodeForTokensAsync(string code, string codeVerifier, string state)
    {
        var httpClient = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");

        var parameters = new Dictionary<string, string>
        {
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["client_id"] = configuration[OAuthConstants.Client]!,
            ["client_secret"] = configuration[OAuthConstants.ClientSecret]!,
            ["redirect_uri"] = configuration[OAuthConstants.RedirectUri]!,
            ["grant_type"] = "authorization_code",
            ["state"] = state
        };

        request.Content = new FormUrlEncodedContent(parameters);

        var response = await httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new BadRequestException($"Failed to exchange Google authorization code: {content}");
        }

        var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(content,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        return tokenResponse ?? throw new BadRequestException("Failed to parse Google token response");
    }

    private static GoogleUserInfo ExtractUserInfoFromIdToken(string idToken)
    {
        var json = ExtractFromToken(idToken);

        var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(
            json,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
            );

        return userInfo ?? throw new BadRequestException("Failed to parse Google user info");
    }

    private static string ExtractFromToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            throw new BadRequestException("Invalid Google ID token format");

        var payload = parts[1];
        var json = Base64UrlDecode(payload);

        return json;
    }

    private async Task<User> FindOrCreateGoogleUserAsync(GoogleUserInfo userInfo)
    {
        return await uow.ExecuteInTransactionAsync(async Task<User> (transaction) =>
        {
            // First try to find by GoogleId
            var user = userManager.Users.FirstOrDefault(u => u.GoogleId == userInfo.Sub);

            if (user is not null)
                return user;

            // Check if a user with this email already exists (registered locally)
            user = await userManager.FindByEmailAsync(userInfo.Email);

            if (user is not null)
            {
                // Link Google account to existing local user
                user.GoogleId = userInfo.Sub;
                user.DisplayName = userInfo.Name;
                await userManager.UpdateAsync(user);
                return user;
            }

            user = new User
            {
                GoogleId = userInfo.Sub,
                Email = userInfo.Email,
                UserName = userInfo.Email,
                DisplayName = userInfo.Name,
                Description = "",
                BannerColor = Color.IndianRed,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user);

            if (!result.Succeeded)
                throw new BadRequestException(string.Join(", ", result.Errors.Select(e => e.Description)));

            await usersRepository.CreateSettingsForUserAsync(user.Id);
            await usersRepository.AssignRolesToUserAsync(user, RoleNames.User);

            await transaction.CommitAsync();

            return user;
        });
    }

    private static string GenerateCodeVerifier()
    {
        return RandomNumberGenerator.GetString(_allowedCodeCharacters, _codeVerifierLength);
    }

    private static string GenerateCodeChallange(string codeVerifier)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(codeVerifier);
        var hash = SHA256.HashData(bytes);
        var encoded = Base64Url.EncodeToString(hash);

        return encoded;
    }

    private static string Base64UrlDecode(string str)
    {
        var decoded = Base64Url.DecodeFromChars(str);
        var utf8 = System.Text.Encoding.UTF8.GetString(decoded);

        return utf8;
    }
}