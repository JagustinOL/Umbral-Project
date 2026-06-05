using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(6)
            .HasConversion(
                value => value.Value,
                value => TeamCode.From(value))
            .IsRequired();

        builder.Property(x => x.CurrentSessionRef)
            .HasColumnName("current_session_ref");

        builder.Property(x => x.IsLocked)
            .HasColumnName("is_locked")
            .IsRequired();

        builder.Property(x => x.IsDisbanded)
            .HasColumnName("is_disbanded")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Metadata
            .FindNavigation(nameof(Team.Members))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey("team_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Team.JoinRequests))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.JoinRequests)
            .WithOne()
            .HasForeignKey("team_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("is_disbanded = false");

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("is_disbanded = false");
    }
}
