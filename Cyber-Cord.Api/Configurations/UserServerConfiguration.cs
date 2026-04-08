using Cyber_Cord.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cyber_Cord.Api.Configurations;

public class UserServerConfiguration : IEntityTypeConfiguration<UserServer>
{
    public void Configure(EntityTypeBuilder<UserServer> builder)
    {
        builder.HasIndex(x => new { x.UserId, x.ServerId })
            .IsUnique();
    }
}