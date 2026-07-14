using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;

namespace ScoringAudit.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SessionRef).HasColumnName("session_ref").IsRequired();
        builder.HasIndex(x => x.SessionRef).IsUnique();
        builder.Property(x => x.MissionRef).HasColumnName("mission_ref").IsRequired();
        builder.Property(x => x.OperatorRef).HasColumnName("operator_ref").IsRequired();
        builder.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc").IsRequired();
        builder.Property(x => x.EndedAtUtc).HasColumnName("ended_at_utc");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsClosed).HasColumnName("is_closed").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasMany(x => x.Events)
            .WithOne()
            .HasForeignKey("AuditLogId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Events)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_events");

        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class SessionEventConfiguration : IEntityTypeConfiguration<SessionEvent>
{
    public void Configure(EntityTypeBuilder<SessionEvent> builder)
    {
        builder.ToTable("session_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").HasConversion<string>().IsRequired();
        builder.Property(x => x.SourceEventId).HasColumnName("source_event_id").IsRequired();
        builder.HasIndex(x => new { x.SessionId, x.SourceEventId }).IsUnique();
        builder.Property(x => x.TeamRef).HasColumnName("team_ref");
        builder.Property(x => x.MissionNodeRef).HasColumnName("mission_node_ref");
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        builder.Property<Guid?>("AuditLogId").HasColumnName("audit_log_id");
    }
}
