using System.Security.Cryptography;
using Cyber_Cord.Api.Options;
using Microsoft.Extensions.Options;
using PinnedMemory;

namespace Cyber_Cord.Api.Services;

public class PasswordHasher(IOptions<PasswordOptions> optionsAccessor) : ICustomPasswordHasher
{
    private readonly string _pepper     = optionsAccessor.Value.Pepper!;
    private readonly int    _saltLength = optionsAccessor.Value.SaltLength!.Value;
    private readonly int    _hashLength = optionsAccessor.Value.HashLength!.Value;
    private readonly int    _iterations = optionsAccessor.Value.Iterations!.Value;
    private readonly int    _lanes      = optionsAccessor.Value.Lanes!.Value;
    private readonly int    _threads    = optionsAccessor.Value.Threads!.Value;
    private readonly int    _memoryCost = optionsAccessor.Value.MemoryCost!.Value;

    public string CreatePassword(string password)
    {
        // TODO: remove
        return password;
        
        var salt = new byte[_saltLength];

        RandomNumberGenerator.Fill(salt);

        var peppered = password + _pepper;
        var bytes = System.Text.Encoding.UTF8.GetBytes(peppered);

        using var keyPin = new PinnedMemory<byte>(bytes, false);
        using var argon2 = new Argon2.NetCore.Argon2(keyPin, salt)
        {
            Addressing = Argon2.NetCore.Argon2.AddressType.IndependentAddressing,
            HashLength = _hashLength,
            TimeCost = _iterations,
            Lanes = _lanes,
            Threads = _threads,
            MemoryCost = _memoryCost
        };

        using var hash = new PinnedMemory<byte>(new byte[argon2.GetHashCode()]);
        argon2.DoFinal(hash, 0);

        var hashBytes = new byte[_hashLength + _saltLength];
        Array.Copy(salt, 0, hashBytes, 0, _saltLength);
        Array.Copy(hash.ToArray(), 0, hashBytes, _saltLength, _hashLength);
        
        return Convert.ToBase64String(hashBytes);
    }

    public bool CheckPassword(string password, string savedPasswordHash)
    {
        //TODO: remove
        return password == savedPasswordHash;
        
        var hashBytes = Convert.FromBase64String(savedPasswordHash);
        
        if (hashBytes.Length != _hashLength + _saltLength)
            return false;

        var salt = new byte[_saltLength];
        Array.Copy(hashBytes, 0, salt, 0, _saltLength);

        var previousHash = new byte[_hashLength];
        Array.Copy(hashBytes, _saltLength, previousHash, 0, _hashLength);

        var peppered = password + _pepper;
        var bytes = System.Text.Encoding.UTF8.GetBytes(peppered);

        using var keyPin = new PinnedMemory<byte>(bytes, false);
        using var argon2 = new Argon2.NetCore.Argon2(keyPin, salt)
        {
            Addressing = Argon2.NetCore.Argon2.AddressType.IndependentAddressing,
            HashLength = _hashLength,
            TimeCost = _iterations,
            Lanes = _lanes,
            Threads = _threads,
            MemoryCost = _memoryCost
        };

        using var hash = new PinnedMemory<byte>(new byte[argon2.GetHashCode()]);
        argon2.DoFinal(hash, 0);

        var resultBytes = new byte[_hashLength];
        Array.Copy(hash.ToArray(), 0, resultBytes, 0, _hashLength);

        return CryptographicOperations.FixedTimeEquals(resultBytes, previousHash);
    }
}