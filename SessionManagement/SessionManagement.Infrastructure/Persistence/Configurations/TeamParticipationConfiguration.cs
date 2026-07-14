using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionManagement.Domain.Entities;

namespace SessionManagement.Infrastructure.Persistence.Configurations;

public sealed class TeamParticipationConfiguration : IEntityTypeConfiguration<TeamParticipation>
{
    public void Configure(EntityTypeBuilder<TeamParticipation> builder)
    {
        builder.ToTable("team_participations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TeamId).HasColumnName("team_id").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");

        builder.HasIndex("LiveSessionId", nameof(TeamParticipation.TeamId)).IsUnique();
    }
}
