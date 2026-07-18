using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Queries.GetOperatorSessionBoard;

public sealed class GetOperatorSessionBoardHandler
    : IRequestHandler<GetOperatorSessionBoardQuery, OperatorSessionBoardDto>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IMissionIntegrationService _missionIntegration;

    public GetOperatorSessionBoardHandler(
        ILiveSessionRepository sessionRepository,
        ITeamRepository teamRepository,
        IMissionIntegrationService missionIntegration)
    {
        _sessionRepository = sessionRepository;
        _teamRepository = teamRepository;
        _missionIntegration = missionIntegration;
    }

    public async Task<OperatorSessionBoardDto> Handle(
        GetOperatorSessionBoardQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdForOperatorAsync(
            request.SessionId, request.OperatorId, cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la sesión {request.SessionId} para el operador {request.OperatorId}.");

        var assigned = await _missionIntegration.GetAssignedMissionsForOperatorAsync(
            request.OperatorId, cancellationToken);
        if (!assigned.Any(x => x.MissionId == session.MissionRef))
            throw new NotFoundException("La misión de la sesión no está asignada al operador.");

        var rules = await NodeValidationRulesFactory.BuildAsync(
            _missionIntegration, session.MissionRef, cancellationToken);

        var teams = await _teamRepository.GetByIdsAsync(session.RegisteredTeamIds, cancellationToken);
        var teamNames = teams.ToDictionary(t => t.Id, t => t.Name);

        var hintsByNode = new Dictionary<Guid, IReadOnlyList<MissionHintData>>();
        var contextByNode = new Dictionary<Guid, (string? NodeType, string? Prompt)>();

        async Task<(string? NodeType, string? Prompt)> GetNodeContextAsync(Guid nodeId)
        {
            if (contextByNode.TryGetValue(nodeId, out var cached))
                return cached;

            var context = await MissionNodeContextHelper.GetContextAsync(
                _missionIntegration, session.MissionRef, nodeId, cancellationToken);
            contextByNode[nodeId] = context;
            return context;
        }

        var entries = new List<OperatorTeamBoardEntryDto>();
        foreach (var teamId in session.RegisteredTeamIds)
        {
            var participation = session.GetTeamParticipationStatus(teamId);
            var currentNodeId = participation is TeamParticipationStatus.Completed or TeamParticipationStatus.Expelled
                ? null
                : session.GetCurrentNodeForTeam(teamId, rules);

            string? nodeType = null;
            int? executionOrder = null;
            string? gameLabel = null;
            var availableHints = Array.Empty<OperatorAvailableHintDto>();

            if (currentNodeId is Guid nodeId)
            {
                var rule = rules.First(r => r.NodeId == nodeId);
                nodeType = rule.ValidationType.ToString();
                executionOrder = rule.ExecutionOrder;

                var (_, nodePrompt) = await GetNodeContextAsync(nodeId);
                gameLabel = string.IsNullOrWhiteSpace(nodePrompt)
                    ? $"{nodeType} · Orden {executionOrder}"
                    : $"{nodeType} · {nodePrompt}";

                var releasedIds = session.ReleasedHints
                    .Where(r => r.TeamId == teamId && r.MissionNodeId == nodeId)
                    .Select(r => r.HintId)
                    .ToHashSet();

                if (!hintsByNode.TryGetValue(nodeId, out var nodeHints))
                {
                    nodeHints = await _missionIntegration.GetHintsForNodeAsync(
                        session.MissionRef, nodeId, cancellationToken);
                    hintsByNode[nodeId] = nodeHints;
                }

                availableHints = nodeHints
                    .Where(h => !releasedIds.Contains(h.Id))
                    .OrderBy(h => h.Order)
                    .Select(h => new OperatorAvailableHintDto(
                        h.Id,
                        h.Order,
                        h.Content,
                        h.PenaltyPoints,
                        nodeType,
                        nodePrompt))
                    .ToArray();
            }

            var teamReleased = session.ReleasedHints
                .Where(r => r.TeamId == teamId)
                .OrderByDescending(r => r.ReleasedAtUtc)
                .ToList();

            var released = new List<OperatorReleasedHintSummaryDto>();
            foreach (var r in teamReleased)
            {
                var (releasedNodeType, releasedPrompt) = await GetNodeContextAsync(r.MissionNodeId);
                released.Add(new OperatorReleasedHintSummaryDto(
                    r.HintId,
                    r.MissionNodeId,
                    r.PenaltyPoints,
                    r.ReleasedAtUtc,
                    r.WasManualRelease,
                    releasedNodeType,
                    releasedPrompt));
            }

            teamNames.TryGetValue(teamId, out var teamName);

            entries.Add(new OperatorTeamBoardEntryDto(
                TeamId: teamId,
                TeamName: teamName,
                ParticipationStatus: participation.ToString(),
                CurrentNodeId: currentNodeId,
                CurrentNodeType: nodeType,
                CurrentExecutionOrder: executionOrder,
                CurrentGameLabel: gameLabel,
                IsMissionCompleted: participation == TeamParticipationStatus.Completed
                    || (participation == TeamParticipationStatus.Active && currentNodeId is null),
                AvailableHints: availableHints,
                ReleasedHints: released));
        }

        return new OperatorSessionBoardDto(
            SessionId: session.Id,
            SessionStatus: session.Status.ToString(),
            StartedAtUtc: session.StartedAtUtc,
            Teams: entries);
    }
}
