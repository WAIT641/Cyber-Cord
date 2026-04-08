using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cyber_Cord.Api.Tests.Helpers;
public static class TestUserManagerFactory
{
    public static UserManager<User> Create(AppDbContext context)
    {
        var userStore = new UserStore<User, IdentityRole<int>, AppDbContext, int>(context);

        var userManager = new UserManager<User>(
            userStore,
            Substitute.For<IOptions<IdentityOptions>>(), 
            Substitute.For<IPasswordHasher<User>>(), 
            [],
            [], 
            Substitute.For<ILookupNormalizer>(),
            Substitute.For<IdentityErrorDescriber>(), 
            Substitute.For<IServiceProvider>(), 
            Substitute.For<ILogger<UserManager<User>>>()
        );

        return userManager;
    }
}
