using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.OperatorSessions.Queries.GetSessionJoinRequests;

public sealed record GetSessionJoinRequestsQuery(
    Guid OperatorId,
    Guid SessionId) : IRequest<IReadOnlyList<SessionJoinRequestDto>>;
