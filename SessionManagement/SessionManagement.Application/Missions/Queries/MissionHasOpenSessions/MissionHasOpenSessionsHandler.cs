using MediatR;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Missions.Queries.MissionHasOpenSessions;

public sealed class MissionHasOpenSessionsHandler : IRequestHandler<MissionHasOpenSessionsQuery, bool>
{
    private readonly ILiveSessionRepository _repository;

    public MissionHasOpenSessionsHandler(ILiveSessionRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> Handle(MissionHasOpenSessionsQuery request, CancellationToken cancellationToken)
        => _repository.HasOpenSessionsByMissionAsync(request.MissionId, cancellationToken);
}
