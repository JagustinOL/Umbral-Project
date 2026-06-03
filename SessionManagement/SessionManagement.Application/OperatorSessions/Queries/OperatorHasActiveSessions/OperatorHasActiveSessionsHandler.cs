using MediatR;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Queries.OperatorHasActiveSessions;

public sealed class OperatorHasActiveSessionsHandler : IRequestHandler<OperatorHasActiveSessionsQuery, bool>
{
    private readonly ILiveSessionRepository _repository;

    public OperatorHasActiveSessionsHandler(ILiveSessionRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> Handle(OperatorHasActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        return _repository.HasOpenSessionsByOperatorAsync(request.OperatorId, cancellationToken);
    }
}
