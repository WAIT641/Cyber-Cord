using Cyber_Cord.Api.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cyber_Cord.Api.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<int>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<int>> builder)
    {
        builder.HasData(
            new IdentityRole<int>
            {
                Id = 1,
                Name = RoleNames.Admin,
                NormalizedName = RoleNames.Admin.ToUpperInvariant(),
                ConcurrencyStamp = "019d4030-c571-7443-8603-22e7c935c890"
            },
            new IdentityRole<int>
            {
                Id = 2,
                Name = RoleNames.User,
                NormalizedName = RoleNames.User.ToUpperInvariant(),
                ConcurrencyStamp = "019d4031-0d0c-7d0f-85de-f62e6b9eaed8"
            }
        );
    }
}

