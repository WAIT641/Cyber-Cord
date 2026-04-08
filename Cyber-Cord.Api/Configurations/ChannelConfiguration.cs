using Cyber_Cord.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared;

namespace Cyber_Cord.Api.Configurations;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(GlobalConstants.MaxNameLength);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(GlobalConstants.MaxDescriptionLength);

        builder.HasOne(c => c.Server)
            .WithMany(s => s.Channels)
            .HasForeignKey(c => c.ServerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Channel)
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}