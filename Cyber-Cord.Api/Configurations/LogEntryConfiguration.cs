using Cyber_Cord.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cyber_Cord.Api.Configurations;

public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    private const int _maxLogMessageLength = 2048;
    private const int _maxLogLevelLength = 32;

    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Message)
            .HasMaxLength(_maxLogMessageLength)
            .IsRequired();

        builder.Property(l => l.MessageTemplate)
            .HasMaxLength(_maxLogMessageLength)
            .IsRequired();

        builder.Property(l => l.Level)
            .HasMaxLength(_maxLogLevelLength)
            .IsRequired();

        builder.Property(l => l.Timestamp)
            .IsRequired();

        builder.Property(l => l.Exception)
            .IsRequired(false);

        builder.Property(l => l.Properties)
            .IsRequired(false);

        builder.Property(l => l.LogEvent)
            .IsRequired(false);
    }
}
