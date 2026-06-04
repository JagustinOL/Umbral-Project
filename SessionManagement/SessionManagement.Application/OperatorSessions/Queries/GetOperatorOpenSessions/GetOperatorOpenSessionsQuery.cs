using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.OperatorSessions.Queries.GetOperatorOpenSessions;

public sealed record GetOperatorOpenSessionsQuery(Guid OperatorId)
    : IRequest<IReadOnlyList<OperatorOpenSessionDto>>;
