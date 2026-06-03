using MediatR;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Queries.OperatorIsSupervisingMission;

public sealed class OperatorIsSupervisingMissionHandler : IRequestHandler<OperatorIsSupervisingMissionQuery, bool>
{
    private readonly ILiveSessionRepository _repository;

    public OperatorIsSupervisingMissionHandler(ILiveSessionRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> Handle(OperatorIsSupervisingMissionQuery request, CancellationToken cancellationToken)
    {
        return _repository.HasOpenSessionForMissionByOperatorAsync(
            request.OperatorId,
            request.MissionId,
            cancellationToken);
    }
}
