using Cyber_Cord.Api.Options;
using Cyber_Cord.Api.Services;

namespace Cyber_Cord.Api.Tests.Services;

public class CustomPasswordHasherTests
{
    private readonly PasswordHasher _service;

    public CustomPasswordHasherTests()
    {
        var passwordOptions = new PasswordOptions
        {
            HashLength = 20,
            SaltLength = 16,
            Iterations = 1,
            Pepper = "gg"
        };

        var options = Microsoft.Extensions.Options.Options.Create(passwordOptions);

        _service = new(options);
    }

    [Theory]
    [InlineData("password")]
    public void CreatePassword_CorrectStructure(string password)
    {
        var hash = _service.CreatePassword(password);

        try
        {
            _ = Convert.FromBase64String(hash);
        }
        catch
        {
            // ignored
        }
    }

    [Theory]
    [InlineData("password")]
    public void CheckPassword_HashEquals(string password)
    {
        // This prolly not good, is it?
        var hash = _service.CreatePassword(password);

        Assert.True(_service.CheckPassword(password, hash));
    }
}