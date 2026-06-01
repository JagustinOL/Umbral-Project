using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionManagement.Domain.Entities;

namespace SessionManagement.Infrastructure.Persistence.Configurations;

public sealed class ReleasedHintConfiguration : IEntityTypeConfiguration<ReleasedHint>
{
    public void Configure(EntityTypeBuilder<ReleasedHint> builder)
    {
        builder.ToTable("released_hints");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property<Guid>("LiveSessionId")
            .HasColumnName("live_session_id")
            .IsRequired();

        builder.Property(x => x.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(x => x.HintId)
            .HasColumnName("hint_id")
            .IsRequired();

        builder.Property(x => x.MissionNodeId)
            .HasColumnName("mission_node_id")
            .IsRequired();

        builder.Property(x => x.ReleasedAtUtc)
            .HasColumnName("released_at_utc")
            .IsRequired();

        builder.Property(x => x.PenaltyPoints)
            .HasColumnName("penalty_points")
            .IsRequired();

        builder.Property(x => x.WasManualRelease)
            .HasColumnName("was_manual_release")
            .IsRequired();
    }
}

