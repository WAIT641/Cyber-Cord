using Cyber_Cord.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared;

namespace Cyber_Cord.Api.Configurations;

public class ServerConfiguration : IEntityTypeConfiguration<Server>
{
    public void Configure(EntityTypeBuilder<Server> builder)
    {
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(GlobalConstants.MaxNameLength);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(GlobalConstants.MaxDescriptionLength);

        builder.Property(x => x.OwnerId)
            .IsRequired();

        builder.HasMany(s => s.UserServers)
            .WithOne(us => us.Server)
            .HasForeignKey(us => us.ServerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Channels)
            .WithOne(c => c.Server)
            .HasForeignKey(c => c.ServerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.BanUserServers)
            .WithOne(bus => bus.Server)
            .HasForeignKey(bus => bus.ServerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}