using FluentValidation;
using MissionManagement.Application.Missions.Commands.CreateMission;

namespace MissionManagement.Application.Missions.Commands.CreateMission;

public sealed class CreateMissionCommandValidator : AbstractValidator<CreateMissionCommand>
{
    public CreateMissionCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.MaxDurationMinutes).GreaterThan(0).When(x => x.MaxDurationMinutes.HasValue);
    }
}
