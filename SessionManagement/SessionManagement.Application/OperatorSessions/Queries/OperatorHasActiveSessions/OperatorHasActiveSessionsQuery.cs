using MediatR;

namespace SessionManagement.Application.OperatorSessions.Queries.OperatorHasActiveSessions;

public sealed record OperatorHasActiveSessionsQuery(Guid OperatorId) : IRequest<bool>;
