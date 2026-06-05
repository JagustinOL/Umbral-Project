using MediatR;
using SessionManagement.Application.Dtos;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Queries.GetOperatorOpenSessions;

public sealed class GetOperatorOpenSessionsHandler
    : IRequestHandler<GetOperatorOpenSessionsQuery, IReadOnlyList<OperatorOpenSessionDto>>
{
    private readonly ILiveSessionRepository _repository;

    public GetOperatorOpenSessionsHandler(ILiveSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<OperatorOpenSessionDto>> Handle(
        GetOperatorOpenSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var sessions = await _repository.GetOpenSessionsByOperatorAsync(
            request.OperatorId,
            cancellationToken);

        return sessions
            .Select(x => new OperatorOpenSessionDto(
                SessionId: x.Id,
                MissionId: x.MissionRef,
                JoinCode: x.JoinCode,
                Status: x.Status.ToString()))
            .ToList();
    }
}
