using System.Security.Claims;
using System.Text;
using Cyber_Cord.Api.Constants;
using Cyber_Cord.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cyber_Cord.Api.Services;

public class TokenProvider(IConfiguration configuration, UserManager<User> manager) : ITokenProvider
{
    public async Task<string> CreateAsync(User user)
    {
        var secretKey = configuration[ConfigurationConstants.SecretKeyPath]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.UserName ?? string.Empty)
        };

        var roles = await manager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>(ConfigurationConstants.TokenExpirationPath)),
            SigningCredentials = credentials,
            Issuer = configuration[ConfigurationConstants.TokenIssuerPath],
            Audience = configuration[ConfigurationConstants.TokenAudiencePath],
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);

        return token;
    }
}
