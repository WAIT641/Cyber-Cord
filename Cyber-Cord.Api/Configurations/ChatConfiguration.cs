using Cyber_Cord.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared;

namespace Cyber_Cord.Api.Configurations;

public class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        // Is not required - 1-to-1 chats do not have a name
        builder.Property(x => x.Name)
            .HasMaxLength(GlobalConstants.MaxNameLength);

        builder.HasMany(c => c.UserChats)
            .WithOne(uc => uc.Chat)
            .HasForeignKey(uc => uc.ChatId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Chat)
            .HasForeignKey(m => m.ChatId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
