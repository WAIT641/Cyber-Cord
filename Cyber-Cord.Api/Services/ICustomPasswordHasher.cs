namespace Cyber_Cord.Api.Services;

public interface ICustomPasswordHasher
{
    string CreatePassword(string password);
    bool CheckPassword(string password, string savedPasswordHash);
}