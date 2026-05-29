using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.ValueObjects;
using System.Text.Json;

namespace MissionManagement.Infrastructure.Persistence.Configurations;

public sealed class MissionConfiguration : IEntityTypeConfiguration<Mission>
{
    public void Configure(EntityTypeBuilder<Mission> builder)
    {
        builder.ToTable("missions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Difficulty)
            .HasColumnName("difficulty")
            .HasConversion(
                v => v.Name,
                v => DifficultyLevel.FromName(v))
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.MaxDurationMinutes)
            .HasColumnName("max_duration_minutes");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc)
            .HasColumnName("last_modified_at_utc");

        builder.HasIndex(x => x.Title)
            .IsUnique();

        builder.Ignore(x => x.Operators);

        builder.Property<List<OperatorRef>>("_operators")
            .HasColumnName("operators")
            .HasColumnType("jsonb")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => string.IsNullOrWhiteSpace(value)
                    ? new List<OperatorRef>()
                    : JsonSerializer.Deserialize<List<OperatorRef>>(value, (JsonSerializerOptions?)null) ?? new List<OperatorRef>());

        builder.Metadata
            .FindNavigation(nameof(Mission.Nodes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Nodes)
            .WithOne()
            .HasForeignKey("MissionId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

