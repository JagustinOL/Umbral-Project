using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Evidence;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.LiveSessions.Queries.GetTeamCurrentNodeContent;

public sealed class GetTeamCurrentNodeContentHandler
    : IRequestHandler<GetTeamCurrentNodeContentQuery, TeamCurrentNodeContentDto>
{
    private readonly ILiveSessionRepository _repository;
    private readonly IMissionIntegrationService _missionIntegrationService;

    public GetTeamCurrentNodeContentHandler(
        ILiveSessionRepository repository,
        IMissionIntegrationService missionIntegrationService)
    {
        _repository = repository;
        _missionIntegrationService = missionIntegrationService;
    }

    public async Task<TeamCurrentNodeContentDto> Handle(
        GetTeamCurrentNodeContentQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró la sesión con Id={request.SessionId}.");

        var rules = await BuildRulesAsync(session.MissionRef, cancellationToken);
        var currentNodeId = session.GetCurrentNodeForTeam(request.TeamId, rules);
        if (currentNodeId is null)
            throw new NotFoundException("El equipo ya completó todos los nodos de la misión.");

        var currentRule = rules.First(x => x.NodeId == currentNodeId.Value);
        var content = await _missionIntegrationService.GetNodePlayerContentAsync(
            session.MissionRef,
            currentNodeId.Value,
            cancellationToken);

        var totalQuestions = currentRule.ValidationType == NodeValidationType.Trivia
            ? currentRule.ExpectedAnswers.Count
            : 1;
        var currentQuestionIndex = currentRule.ValidationType == NodeValidationType.Trivia
            ? NodeProgressHelper.GetNextQuestionIndex(
                session,
                request.TeamId,
                currentNodeId.Value,
                totalQuestions)
            : 0;

        return new TeamCurrentNodeContentDto(
            NodeId: content.NodeId,
            NodeType: content.NodeType,
            Questions: content.Questions?
                .Select(q => new PlayerTriviaQuestionDto(q.Prompt, q.Options))
                .ToList(),
            Instructions: content.Instructions,
            CurrentQuestionIndex: currentQuestionIndex,
            TotalQuestions: totalQuestions);
    }

    private async Task<IReadOnlyList<NodeValidationRule>> BuildRulesAsync(
        Guid missionId,
        CancellationToken cancellationToken)
    {
        var data = await _missionIntegrationService.GetNodeValidationDataAsync(missionId, cancellationToken);
        return data
            .Select(x => new NodeValidationRule(
                NodeId: x.NodeId,
                ExecutionOrder: x.ExecutionOrder,
                ValidationType: ParseType(x.NodeType),
                ExpectedAnswers: x.ExpectedAnswers))
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
