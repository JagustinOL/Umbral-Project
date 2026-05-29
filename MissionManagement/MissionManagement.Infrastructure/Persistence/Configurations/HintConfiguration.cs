using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MissionManagement.Domain.Entities;

namespace MissionManagement.Infrastructure.Persistence.Configurations;

public sealed class HintConfiguration : IEntityTypeConfiguration<Hint>
{
    public void Configure(EntityTypeBuilder<Hint> builder)
    {
        builder.ToTable("hints");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MissionNodeId)
            .HasColumnName("mission_node_id")
            .IsRequired();

        builder.Property(x => x.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.PenaltyPoints)
            .HasColumnName("penalty_points")
            .IsRequired();

        builder.HasIndex(x => new { x.MissionNodeId, x.Order })
            .IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_hints_order_positive", "\"order\" >= 1");
            t.HasCheckConstraint("ck_hints_penalty_non_negative", "penalty_points >= 0");
        });
    }
}

