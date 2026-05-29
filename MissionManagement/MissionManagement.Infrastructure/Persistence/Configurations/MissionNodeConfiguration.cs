using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.ValueObjects;
using System.Text.Json;

namespace MissionManagement.Infrastructure.Persistence.Configurations;

public sealed class MissionNodeConfiguration : IEntityTypeConfiguration<MissionNode>
{
    public void Configure(EntityTypeBuilder<MissionNode> builder)
    {
        builder.ToTable("mission_nodes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property<Guid>("MissionId").HasColumnName("mission_id");

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.NodeType)
            .HasColumnName("node_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ExecutionOrder)
            .HasColumnName("execution_order")
            .IsRequired();

        builder.Property(x => x.BaseScore)
            .HasColumnName("base_score")
            .IsRequired();

        builder.Property(x => x.ParentNodeId)
            .HasColumnName("parent_node_id");

        builder.Property(x => x.Instructions)
            .HasColumnName("instructions")
            .HasMaxLength(2000);

        builder.Property(x => x.SecretCode)
            .HasColumnName("secret_code")
            .HasMaxLength(250);

        builder.ComplexProperty(
            x => x.Destination,
            destinationBuilder =>
            {
                destinationBuilder.Property(x => x.Latitude)
                    .HasColumnName("destination_latitude");
                destinationBuilder.Property(x => x.Longitude)
                    .HasColumnName("destination_longitude");
            });

        builder.Property(x => x.TriviaQuestions)
            .HasColumnName("trivia_questions")
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => string.IsNullOrWhiteSpace(value)
                    ? new List<TriviaQuestion>()
                    : JsonSerializer.Deserialize<List<TriviaQuestion>>(value, (JsonSerializerOptions?)null) ?? new List<TriviaQuestion>());

        builder.HasIndex("MissionId", "ParentNodeId", nameof(MissionNode.ExecutionOrder))
            .IsUnique();

        builder.Metadata
            .FindNavigation(nameof(MissionNode.Children))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Children)
            .WithOne()
            .HasForeignKey(x => x.ParentNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(MissionNode.Hints))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Hints)
            .WithOne()
            .HasForeignKey(x => x.MissionNodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

