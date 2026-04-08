using Cyber_Cord.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cyber_Cord.Api.Configurations;

public class BanUserServerConfiguration : IEntityTypeConfiguration<BanUserServer>
{
    public void Configure(EntityTypeBuilder<BanUserServer> builder)
    {
        builder.HasOne(b => b.User)
            .WithMany(u => u.BanUserServers)
            .HasForeignKey(b => b.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Server)
            .WithMany(s => s.BanUserServers)
            .HasForeignKey(b => b.ServerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent duplicate bans
        builder.HasIndex(x => new { x.UserId, x.ServerId })
            .IsUnique();
    }
}