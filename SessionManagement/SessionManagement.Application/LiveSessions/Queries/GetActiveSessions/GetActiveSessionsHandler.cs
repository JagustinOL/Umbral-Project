using MediatR;
using SessionManagement.Application.Dtos;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.LiveSessions.Queries.GetActiveSessions;

public sealed class GetActiveSessionsHandler : IRequestHandler<GetActiveSessionsQuery, IReadOnlyList<ActiveSessionDto>>
{
    private readonly ILiveSessionRepository _repository;

    public GetActiveSessionsHandler(ILiveSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ActiveSessionDto>> Handle(GetActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _repository.GetActiveSessionsAsync(cancellationToken);

        return sessions
            .Select(x => new ActiveSessionDto(
                SessionId: x.Id,
                MissionRef: x.MissionRef,
                JoinCode: x.JoinCode,
                Status: x.Status.ToString(),
                CreatedAtUtc: x.CreatedAtUtc))
            .ToList();
    }
}

