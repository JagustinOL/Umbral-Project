using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.OperatorSessions.Queries.GetSessionTeams;

public sealed record GetSessionTeamsQuery(
    Guid OperatorId,
    Guid SessionId
) : IRequest<SessionTeamsDto>;

