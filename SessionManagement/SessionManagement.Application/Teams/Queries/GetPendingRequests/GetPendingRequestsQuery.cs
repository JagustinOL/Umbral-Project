using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.Teams.Queries.GetPendingRequests;

public sealed record GetPendingRequestsQuery(
    Guid TeamId,
    Guid RequestorId
) : IRequest<IReadOnlyList<JoinRequestDto>>;
