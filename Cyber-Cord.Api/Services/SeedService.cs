using Cyber_Cord.Api.Constants;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Repositories;

namespace Cyber_Cord.Api.Services;

public static class SeedService
{
    public async static void SeedAdminAsync(IServiceProvider serviceProvider)
    {
        var scope = serviceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IUsersRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<ICustomPasswordHasher>();

        var existingUser = await repository.SearchSingularAsync(new()
        {
            SearchName = "admin",
            SingleResultOnly = true
        });

        if (existingUser is null)
        {
            var userModel = new UserCreateModel
            {
                BannerColor = new ColorCreateModel
                {
                    Red = 255,
                    Green = 0,
                    Blue = 0
                },
                Description = "Initial admin",
                DisplayName = "Admin",
                Email = "admin@example.com",
                Name = "admin",
            };

            var passwordHash = hasher.CreatePassword("admin");

            var user = await repository.CreateUserAsync(userModel, passwordHash);
            await repository.CreateSettingsForUserAsync(user.Id);

            await repository.AssignRolesToUserAsync(user, RoleNames.Admin, RoleNames.User);

            await repository.ValidateUserAsync(user);
        }
    }
}
