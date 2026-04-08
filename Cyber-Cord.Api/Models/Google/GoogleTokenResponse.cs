namespace Cyber_Cord.Api.Models.Google;

public class GoogleTokenResponse
{
    public string AccessToken { get; set; } = default!;
    public string IdToken { get; set; } = default!;
    public string TokenType { get; set; } = default!;
    public int ExpiresIn { get; set; }

}

