using FluentAssertions;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.ValueObjects;

public sealed class TriviaQuestionTests
{
    [Fact]
    public void Constructor_WhenPromptIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        const string prompt = " ";
        IReadOnlyList<string> options = ["A", "B"];

        // Act
        var action = () => new TriviaQuestion(prompt, options, correctOptionIndex: 0);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*no puede estar vacia*");
    }

    [Fact]
    public void Constructor_WhenOptionsCountIsLessThanTwo_ThrowsArgumentException()
    {
        // Arrange
        IReadOnlyList<string> options = ["A"];

        // Act
        var action = () => new TriviaQuestion("Pregunta", options, correctOptionIndex: 0);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*al menos dos opciones*");
    }

    [Fact]
    public void Constructor_WhenCorrectOptionIndexIsOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        IReadOnlyList<string> options = ["A", "B"];

        // Act
        var action = () => new TriviaQuestion("Pregunta", options, correctOptionIndex: 2);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*respuesta correcta debe existir*");
    }

    [Fact]
    public void Constructor_WhenDataIsValid_CreatesTriviaQuestion()
    {
        // Arrange
        IReadOnlyList<string> options = ["  A  ", "  B  "];

        // Act
        var result = new TriviaQuestion("  Pregunta  ", options, correctOptionIndex: 1);

        // Assert
        result.Prompt.Should().Be("Pregunta");
        result.Options.Should().ContainInOrder("A", "B");
        result.CorrectOptionIndex.Should().Be(1);
    }
}

