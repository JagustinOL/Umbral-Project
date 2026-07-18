using FluentAssertions;
using FluentValidation.TestHelper;
using MissionManagement.Application.Missions.Commands.CreateMission;
using Xunit;

namespace MissionManagement.Application.Tests.Missions.Commands.CreateMission;

public sealed class CreateMissionCommandValidatorTests
{
    private readonly CreateMissionCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_Passes()
    {
        var result = _validator.TestValidate(
            new CreateMissionCommand("Título", "Descripción válida", Difficulty: 1, MaxDurationMinutes: null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenTitleEmpty_Fails()
    {
        var result = _validator.TestValidate(
            new CreateMissionCommand("", "Descripción", 1, null));

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenDescriptionEmpty_Fails()
    {
        var result = _validator.TestValidate(
            new CreateMissionCommand("Título", "", 1, null));

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenMaxDurationNotPositive_Fails()
    {
        var result = _validator.TestValidate(
            new CreateMissionCommand("Título", "Descripción", 1, 0));

        result.ShouldHaveValidationErrorFor(x => x.MaxDurationMinutes);
    }
}
