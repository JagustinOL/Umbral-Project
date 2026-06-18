using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.ValueObjects;
using System.Text.Json;

namespace ScoringAudit.Infrastructure.Persistence.Configurations;

public sealed class TeamLedgerConfiguration : IEntityTypeConfiguration<TeamLedger>
{
    public void Configure(EntityTypeBuilder<TeamLedger> builder)
    {
        builder.ToTable("team_ledgers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TeamRef).HasColumnName("team_ref").IsRequired();
        builder.Property(x => x.SessionRef).HasColumnName("session_ref").IsRequired();
        builder.Property(x => x.TeamName).HasColumnName("team_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsClosed).HasColumnName("is_closed").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasMany(x => x.Entries)
            .WithOne()
            .HasForeignKey("TeamLedgerId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_entries");

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.TotalScore);
        builder.Ignore(x => x.CompletedNodesCount);
        builder.Ignore(x => x.PenaltiesCount);
        builder.Ignore(x => x.LastPositiveEntryElapsedSeconds);
    }
}

public sealed class ScoreEntryConfiguration : IEntityTypeConfiguration<ScoreEntry>
{
    public void Configure(EntityTypeBuilder<ScoreEntry> builder)
    {
        builder.ToTable("score_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Points).HasColumnName("points").IsRequired();
        builder.Property(x => x.RecordedAtUtc).HasColumnName("recorded_at_utc").IsRequired();
        builder.Property(x => x.EntryType).HasColumnName("entry_type").HasConversion<string>().IsRequired();
        builder.Property(x => x.SourceEventId).HasColumnName("source_event_id").IsRequired();
        builder.Property<Guid?>("TeamLedgerId").HasColumnName("team_ledger_id");

        builder.Property(x => x.Origin)
            .HasColumnName("origin")
            .HasColumnType("jsonb")
            .HasConversion(
                value => value == null ? null : JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => string.IsNullOrWhiteSpace(value)
                    ? null
                    : JsonSerializer.Deserialize<ScoreOrigin>(value, (JsonSerializerOptions?)null));

        builder.Property(x => x.PenaltyReason)
            .HasColumnName("penalty_reason")
            .HasColumnType("jsonb")
            .HasConversion(
                value => value == null ? null : JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => string.IsNullOrWhiteSpace(value)
                    ? null
                    : JsonSerializer.Deserialize<PenaltyReason>(value, (JsonSerializerOptions?)null));
    }
}
