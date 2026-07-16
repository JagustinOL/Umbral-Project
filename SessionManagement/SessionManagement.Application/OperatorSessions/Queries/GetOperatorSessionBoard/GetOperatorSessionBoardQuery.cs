using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.OperatorSessions.Queries.GetOperatorSessionBoard;

public sealed record GetOperatorSessionBoardQuery(
    Guid OperatorId,
    Guid SessionId) : IRequest<OperatorSessionBoardDto>;
