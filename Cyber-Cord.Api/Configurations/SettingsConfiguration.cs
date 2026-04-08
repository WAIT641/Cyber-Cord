using Cyber_Cord.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cyber_Cord.Api.Configurations;

public class SettingsConfiguration : IEntityTypeConfiguration<Settings>
{
    public void Configure(EntityTypeBuilder<Settings> builder)
    {
        builder.Property(s => s.EnableSounds)
            .IsRequired();

        builder.HasOne(s => s.User)
            .WithOne(u => u.Settings)
            .HasForeignKey<User>(u => u.SettingsId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
