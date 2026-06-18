using FluentValidation;
using SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;

namespace SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;

public sealed class CreateLiveSessionCommandValidator : AbstractValidator<CreateLiveSessionCommand>
{
    public CreateLiveSessionCommandValidator()
    {
        RuleFor(x => x.OperatorId).NotEmpty();
        RuleFor(x => x.MissionId).NotEmpty();
    }
}
