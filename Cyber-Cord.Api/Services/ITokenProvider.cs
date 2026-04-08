using Cyber_Cord.Api.Entities;

namespace Cyber_Cord.Api.Services;

public interface ITokenProvider
{
    Task<string> CreateAsync(User user);
}