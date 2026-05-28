using FluentAssertions;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.Aggregates;

public sealed class MissionTests
{
    [Fact]
    public void UpdateDetails_WhenStatusIsActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Misión activa", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc etapa", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        mission.Activate();

        // Act
        var act = () => mission.UpdateDetails("Nuevo título", "Nueva descripción", maxDurationMinutes: 60);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*estado actual es 'Active'*");
    }

    [Fact]
    public void AddRootNode_WhenNodeTypeIsNotStage_ThrowsInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Misión borrador", "Descripción", DifficultyLevel.Easy);
        var triviaNode = MissionNode.Create(
            "Trivia 1",
            "Pregunta",
            MissionNodeType.Trivia,
            executionOrder: 1,
            baseScore: 5);

        // Act
        var act = () => mission.AddRootNode(triviaNode);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tipo 'Stage'*");
    }

    [Fact]
    public void Activate_WhenNoNodesExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Misión vacía", "Sin etapas", DifficultyLevel.Hard);

        // Act
        var act = () => mission.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no tiene ningún nodo*");
    }

    [Fact]
    public void Create_WhenValidData_InitializesAsDraft()
    {
        // Arrange
        // Act
        var mission = Mission.Create("Nueva misión", "Descripción válida", DifficultyLevel.Medium, maxDurationMinutes: 90);

        // Assert
        mission.Status.Should().Be(MissionStatus.Draft);
        mission.Title.Should().Be("Nueva misión");
        mission.Nodes.Should().BeEmpty();
    }

    [Fact]
    public void AddRootNode_WhenDraftAndStageNode_AddsNodeSuccessfully()
    {
        // Arrange
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 0);

        // Act
        mission.AddRootNode(stage);

        // Assert
        mission.Nodes.Should().HaveCount(1);
        mission.Nodes[0].NodeType.Should().Be(MissionNodeType.Stage);
    }

    [Fact]
    public void Activate_WhenHasAtLeastOneNode_SetsStatusToActive()
    {
        // Arrange
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);

        // Act
        mission.Activate();

        // Assert
        mission.Status.Should().Be(MissionStatus.Active);
        mission.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "MissionActivatedEvent");
    }
}
