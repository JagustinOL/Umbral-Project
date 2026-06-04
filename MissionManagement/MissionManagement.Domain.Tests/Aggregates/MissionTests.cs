using FluentAssertions;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.Aggregates;

public sealed class MissionTests
{
    [Fact]
    public void AssignOperator_WhenOperatorAlreadyAssigned_ThrowsInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        var operatorId = Guid.NewGuid();
        mission.AssignOperator(operatorId);

        // Act
        var action = () => mission.AssignOperator(operatorId);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*ya está asignado*");
    }

    [Fact]
    public void AssignOperator_WhenValid_AddsOperatorToList()
    {
        // Arrange
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        var operatorId = Guid.NewGuid();

        // Act
        mission.AssignOperator(operatorId);

        // Assert
        mission.Operators.Should().ContainSingle(x => x.OperatorId == operatorId);
    }

    [Fact]
    public void AssignOperator_WhenDraftWithNodes_ActivatesMission()
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        var operatorId = Guid.NewGuid();

        mission.AssignOperator(operatorId);

        mission.Status.Should().Be(MissionStatus.Active);
        mission.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "MissionActivatedEvent");
    }

    [Fact]
    public void AssignOperator_WhenDraftWithoutNodes_ThrowsInvalidOperationException()
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var operatorId = Guid.NewGuid();

        var action = () => mission.AssignOperator(operatorId);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*no tiene ningún nodo*");
        mission.Status.Should().Be(MissionStatus.Draft);
    }

    [Fact]
    public void RevokeOperator_WhenLastOperatorOnActiveMission_ReturnsToDraft()
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        var operatorId = Guid.NewGuid();
        mission.AssignOperator(operatorId);
        mission.Status.Should().Be(MissionStatus.Active);

        mission.RevokeOperator(operatorId);

        mission.Operators.Should().BeEmpty();
        mission.Status.Should().Be(MissionStatus.Draft);
    }

    [Fact]
    public void RevokeOperator_WhenOperatorDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var operatorId = Guid.NewGuid();

        // Act
        var action = () => mission.RevokeOperator(operatorId);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*no está asignado*");
    }

    [Fact]
    public void AddTriviaNode_WhenMissionIsActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Misión activa", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc etapa", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        mission.Activate();

        IReadOnlyList<TriviaQuestion> questions =
        [
            new TriviaQuestion("¿Capital de Colombia?", ["Bogotá", "Medellín"], correctOptionIndex: 0)
        ];

        // Act
        var action = () => mission.AddTriviaNode(
            parentNodeId: stage.Id,
            questions: questions,
            executionOrder: 1);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*estado actual es 'Active'*");
    }

    [Fact]
    public void AddHint_WhenMissionIsActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var mission = Mission.Create("Misión activa", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc etapa", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        var triviaId = mission.AddTriviaNode(
            stage.Id,
            [new TriviaQuestion("¿Pregunta?", ["A", "B"], correctOptionIndex: 0)],
            executionOrder: 1);
        mission.Activate();

        var hint = Hint.Create(
            missionNodeId: triviaId,
            order: 1,
            content: "Pista inicial");

        // Act
        var action = () => mission.AddHintToNode(triviaId, hint);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*estado actual es 'Active'*");
    }

    [Fact]
    public void AddHintToNode_WhenNodeIsStage_ThrowsInvalidOperationException()
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc etapa", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);

        var hint = Hint.Create(stage.Id, order: 1, content: "Pista en etapa");

        var action = () => mission.AddHintToNode(stage.Id, hint);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Trivia*TreasureHunt*");
    }

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
