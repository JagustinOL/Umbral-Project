using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.ValueObjects;
using System.Text.Json;

namespace SessionManagement.Infrastructure.Persistence.Configurations;

public sealed class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> builder)
    {
        builder.ToTable("live_sessions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MissionRef)
            .HasColumnName("mission_ref")
            .IsRequired();

        builder.Property(x => x.OperatorRef)
            .HasColumnName("operator_ref")
            .IsRequired();

        builder.Property(x => x.JoinCode)
            .HasColumnName("join_code")
            .HasMaxLength(6)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .HasColumnName("started_at_utc");

        builder.Property(x => x.FinalizedAtUtc)
            .HasColumnName("finalized_at_utc");

        builder.Property(x => x.MaxDurationMinutes)
            .HasColumnName("max_duration_minutes");

        builder.Property(x => x.DifficultyMultiplier)
            .HasColumnName("difficulty_multiplier")
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property<List<Guid>>("_registeredTeamIds")
            .HasColumnName("registered_team_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => string.IsNullOrWhiteSpace(value)
                    ? new List<Guid>()
                    : JsonSerializer.Deserialize<List<Guid>>(value, (JsonSerializerOptions?)null) ?? new List<Guid>());

        builder.Property<List<AllowedNode>>("_allowedNodes")
            .HasColumnName("allowed_nodes")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => string.IsNullOrWhiteSpace(value)
                    ? new List<AllowedNode>()
                    : JsonSerializer.Deserialize<List<AllowedNode>>(value, (JsonSerializerOptions?)null) ?? new List<AllowedNode>());

        builder.Metadata
            .FindNavigation(nameof(LiveSession.EvidenceSubmissions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.EvidenceSubmissions)
            .WithOne()
            .HasForeignKey("LiveSessionId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(LiveSession.ReleasedHints))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.ReleasedHints)
            .WithOne()
            .HasForeignKey("LiveSessionId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(LiveSession.JoinRequests))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.JoinRequests)
            .WithOne()
            .HasForeignKey("LiveSessionId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(LiveSession.TeamParticipations))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.TeamParticipations)
            .WithOne()
            .HasForeignKey("LiveSessionId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.JoinCode).IsUnique();
    }
}

