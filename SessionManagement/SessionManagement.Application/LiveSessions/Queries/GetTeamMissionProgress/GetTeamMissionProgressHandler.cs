using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Evidence;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.LiveSessions.Queries.GetTeamMissionProgress;

public sealed class GetTeamMissionProgressHandler
    : IRequestHandler<GetTeamMissionProgressQuery, TeamMissionProgressDto>
{
    private readonly ILiveSessionRepository _repository;
    private readonly IMissionIntegrationService _missionIntegrationService;

    public GetTeamMissionProgressHandler(
        ILiveSessionRepository repository,
        IMissionIntegrationService missionIntegrationService)
    {
        _repository = repository;
        _missionIntegrationService = missionIntegrationService;
    }

    public async Task<TeamMissionProgressDto> Handle(
        GetTeamMissionProgressQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró la sesión con Id={request.SessionId}.");

        if (!session.RegisteredTeamIds.Contains(request.TeamId))
            throw new NotFoundException(
                $"El equipo {request.TeamId} no está registrado en la sesión {request.SessionId}.");

        var rules = await NodeValidationRulesFactory.BuildAsync(
            _missionIntegrationService, session.MissionRef, cancellationToken);

        var allowedById = session.AllowedNodes.ToDictionary(n => n.NodeId);
        var nodes = new List<MissionProgressNodeDto>(rules.Count);

        foreach (var rule in rules.OrderBy(r => r.ExecutionOrder))
        {
            allowedById.TryGetValue(rule.NodeId, out var allowed);
            var title = string.IsNullOrWhiteSpace(allowed?.Title)
                ? $"{rule.ValidationType} · Nodo #{rule.ExecutionOrder}"
                : allowed!.Title;
            var nodeType = allowed?.NodeType ?? rule.ValidationType.ToString();
            var baseScore = allowed?.BaseScore ?? 0;
            var isCompleted = NodeProgressHelper.IsNodeCompleted(session, request.TeamId, rule);

            nodes.Add(new MissionProgressNodeDto(
                NodeId: rule.NodeId,
                ExecutionOrder: rule.ExecutionOrder,
                NodeType: nodeType,
                Title: title,
                BaseScore: baseScore,
                IsCompleted: isCompleted));
        }

        var completedNodes = nodes.Count(n => n.IsCompleted);
        var isMissionCompleted = nodes.Count > 0 && completedNodes == nodes.Count;

        return new TeamMissionProgressDto(
            SessionId: session.Id,
            TeamId: request.TeamId,
            IsMissionCompleted: isMissionCompleted,
            CompletedNodes: completedNodes,
            TotalNodes: nodes.Count,
            Nodes: nodes);
    }
}
