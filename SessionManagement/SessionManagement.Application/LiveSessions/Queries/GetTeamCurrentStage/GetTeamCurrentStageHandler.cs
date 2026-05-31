using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.LiveSessions.Queries.GetTeamCurrentStage;

public sealed class GetTeamCurrentStageHandler : IRequestHandler<GetTeamCurrentStageQuery, TeamCurrentStageDto>
{
    private readonly ILiveSessionRepository _repository;
    private readonly IMissionIntegrationService _missionIntegrationService;

    public GetTeamCurrentStageHandler(
        ILiveSessionRepository repository,
        IMissionIntegrationService missionIntegrationService)
    {
        _repository = repository;
        _missionIntegrationService = missionIntegrationService;
    }

    public async Task<TeamCurrentStageDto> Handle(GetTeamCurrentStageQuery request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró la sesión con Id={request.SessionId}.");

        var rules = await BuildRulesAsync(session.MissionRef, cancellationToken);
        var currentNodeId = session.GetCurrentNodeForTeam(request.TeamId, rules);

        if (currentNodeId is null)
        {
            return new TeamCurrentStageDto(
                SessionId: session.Id,
                TeamId: request.TeamId,
                CurrentNodeId: null,
                CurrentNodeType: null,
                CurrentExecutionOrder: null,
                IsCompleted: true);
        }

        var currentRule = rules.First(x => x.NodeId == currentNodeId.Value);

        return new TeamCurrentStageDto(
            SessionId: session.Id,
            TeamId: request.TeamId,
            CurrentNodeId: currentRule.NodeId,
            CurrentNodeType: currentRule.ValidationType.ToString(),
            CurrentExecutionOrder: currentRule.ExecutionOrder,
            IsCompleted: false);
    }

    private async Task<IReadOnlyList<NodeValidationRule>> BuildRulesAsync(Guid missionId, CancellationToken cancellationToken)
    {
        var data = await _missionIntegrationService.GetNodeValidationDataAsync(missionId, cancellationToken);
        return data
            .Select(x => new NodeValidationRule(
                NodeId: x.NodeId,
                ExecutionOrder: x.ExecutionOrder,
                ValidationType: ParseType(x.NodeType),
                ExpectedValue: x.ExpectedValue))
            .OrderBy(x => x.ExecutionOrder)
            .ToList();
    }

    private static NodeValidationType ParseType(string rawType)
    {
        if (string.Equals(rawType, "Trivia", StringComparison.OrdinalIgnoreCase))
            return NodeValidationType.Trivia;
        if (string.Equals(rawType, "TreasureHunt", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rawType, "Treasure_Hunt", StringComparison.OrdinalIgnoreCase))
            return NodeValidationType.TreasureHunt;

        throw new InvalidOperationException($"Tipo de nodo no soportado: '{rawType}'.");
    }
}

