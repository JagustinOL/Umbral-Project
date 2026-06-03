using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;

public sealed record CreateLiveSessionCommand(
    Guid OperatorId,
    Guid MissionId
) : IRequest<CreatedLiveSessionDto>;

