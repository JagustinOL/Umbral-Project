using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionManagement.Domain.Entities;

namespace SessionManagement.Infrastructure.Persistence.Configurations;

public sealed class SessionJoinRequestConfiguration : IEntityTypeConfiguration<SessionJoinRequest>
{
    public void Configure(EntityTypeBuilder<SessionJoinRequest> builder)
    {
        builder.ToTable("session_join_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TeamId).HasColumnName("team_id").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(x => x.RequestedAtUtc).HasColumnName("requested_at_utc").IsRequired();
        builder.Property(x => x.ResolvedAtUtc).HasColumnName("resolved_at_utc");
        builder.Property(x => x.ResolvedByOperatorId).HasColumnName("resolved_by_operator_id");

        builder.HasIndex("LiveSessionId", nameof(SessionJoinRequest.TeamId));
    }
}
