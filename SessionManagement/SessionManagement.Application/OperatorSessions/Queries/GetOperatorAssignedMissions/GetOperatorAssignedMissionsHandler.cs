using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.OperatorSessions.Queries.GetOperatorAssignedMissions;

public sealed class GetOperatorAssignedMissionsHandler : IRequestHandler<GetOperatorAssignedMissionsQuery, IReadOnlyList<OperatorAssignedMissionDto>>
{
    private readonly IMissionIntegrationService _missionIntegrationService;

    public GetOperatorAssignedMissionsHandler(IMissionIntegrationService missionIntegrationService)
    {
        _missionIntegrationService = missionIntegrationService;
    }

    public async Task<IReadOnlyList<OperatorAssignedMissionDto>> Handle(
        GetOperatorAssignedMissionsQuery request,
        CancellationToken cancellationToken)
    {
        var missions = await _missionIntegrationService.GetAssignedMissionsForOperatorAsync(
            request.OperatorId,
            cancellationToken);

        return missions
            .Select(x => new OperatorAssignedMissionDto(
                MissionId: x.MissionId,
                OperatorId: x.OperatorId,
                Title: x.Title))
            .ToList();
    }
}

