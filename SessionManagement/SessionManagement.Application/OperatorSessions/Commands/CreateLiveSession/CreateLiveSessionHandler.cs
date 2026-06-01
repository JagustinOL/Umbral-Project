using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;

public sealed class CreateLiveSessionHandler : IRequestHandler<CreateLiveSessionCommand, CreatedLiveSessionDto>
{
    private readonly ILiveSessionRepository _repository;
    private readonly IMissionIntegrationService _missionIntegrationService;

    public CreateLiveSessionHandler(
        ILiveSessionRepository repository,
        IMissionIntegrationService missionIntegrationService)
    {
        _repository = repository;
        _missionIntegrationService = missionIntegrationService;
    }

    public async Task<CreatedLiveSessionDto> Handle(CreateLiveSessionCommand request, CancellationToken cancellationToken)
    {
        var assignedMissions = await _missionIntegrationService.GetAssignedMissionsForOperatorAsync(
            request.OperatorId,
            cancellationToken);

        bool hasAccess = assignedMissions.Any(x => x.MissionId == request.MissionId);
        if (!hasAccess)
            throw new NotFoundException("La misión no está asignada al operador (RN-16).");

        var nodeValidationData = await _missionIntegrationService.GetNodeValidationDataAsync(request.MissionId, cancellationToken);
        var allowedNodes = nodeValidationData
            .OrderBy(x => x.ExecutionOrder)
            .Select(x => new AllowedNode(
                NodeId: x.NodeId,
                NodeType: x.NodeType,
                BaseScore: ResolveBaseScore(x.NodeType)))
            .ToList();

        var session = LiveSession.CreateForMission(
            missionRef: request.MissionId,
            operatorRef: request.OperatorId,
            allowedNodes: allowedNodes,
            difficultyMultiplier: 1.0m);

        await _repository.SaveAsync(session, cancellationToken);

        return new CreatedLiveSessionDto(
            SessionId: session.Id,
            JoinCode: session.JoinCode);
    }

    private static int ResolveBaseScore(string nodeType)
    {
        if (string.Equals(nodeType, "TreasureHunt", StringComparison.OrdinalIgnoreCase))
            return 150;

        return 100;
    }
}

