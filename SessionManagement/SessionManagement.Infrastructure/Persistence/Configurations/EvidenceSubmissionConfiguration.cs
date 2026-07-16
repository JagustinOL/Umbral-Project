using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionManagement.Domain.Entities;

namespace SessionManagement.Infrastructure.Persistence.Configurations;

public sealed class EvidenceSubmissionConfiguration : IEntityTypeConfiguration<EvidenceSubmission>
{
    public void Configure(EntityTypeBuilder<EvidenceSubmission> builder)
    {
        builder.ToTable("evidence_submissions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property<Guid>("LiveSessionId")
            .HasColumnName("live_session_id")
            .IsRequired();

        builder.Property(x => x.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(x => x.MissionNodeId)
            .HasColumnName("mission_node_id")
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.QuestionIndex)
            .HasColumnName("question_index");

        builder.Property(x => x.SubmittedAtUtc)
            .HasColumnName("submitted_at_utc")
            .IsRequired();

        builder.Property(x => x.IsValid)
            .HasColumnName("is_valid");

        builder.Property(x => x.ValidatedAtUtc)
            .HasColumnName("validated_at_utc");

        builder.Property(x => x.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(500);
    }
}

