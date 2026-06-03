using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionManagement.Domain.Entities;

namespace SessionManagement.Infrastructure.Persistence.Configurations;

public sealed class JoinRequestConfiguration : IEntityTypeConfiguration<JoinRequest>
{
    public void Configure(EntityTypeBuilder<JoinRequest> builder)
    {
        builder.ToTable("team_join_requests");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property<Guid>("team_id")
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(x => x.PlayerRef)
            .HasColumnName("player_ref")
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.RequestedAtUtc)
            .HasColumnName("requested_at_utc")
            .IsRequired();

        builder.Property(x => x.ReviewedAtUtc)
            .HasColumnName("reviewed_at_utc");

        builder.HasIndex("team_id", nameof(JoinRequest.PlayerRef), nameof(JoinRequest.Status));
    }
}
