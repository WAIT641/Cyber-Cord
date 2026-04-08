using Cyber_Cord.Api.Conversions;
using Cyber_Cord.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared;

namespace Cyber_Cord.Api.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(GlobalConstants.MaxNameLength);

        builder.Property(u => u.Description)
            .IsRequired()
            .HasMaxLength(GlobalConstants.MaxDescriptionLength);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.IsActivated)
            .IsRequired();

        builder.Property(u => u.BannerColor)
            .IsRequired()
            .HasConversion<ColorToUint32Converter>();

        builder.HasMany(u => u.UserChats)
            .WithOne(uc => uc.User)
            .HasForeignKey(uc => uc.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Messages)
            .WithOne(m => m.User)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.SetNull); // Messages should not be deleted when a user is 

        builder.HasMany(u => u.ReceivedFriendships)
            .WithOne(f => f.ReceivingUser)
            .HasForeignKey(f => f.ReceivingId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.RequestedFriendships)
            .WithOne(f => f.RequestingUser)
            .HasForeignKey(f => f.RequestingId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
