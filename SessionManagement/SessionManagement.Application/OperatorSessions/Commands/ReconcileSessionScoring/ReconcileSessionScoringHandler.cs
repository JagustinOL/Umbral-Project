using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Common;
using SessionManagement.Domain.Events;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Commands.ReconcileSessionScoring;

/// <summary>
/// Reemite eventos de scoring (equipos registrados + evidencias válidas)
/// para reconstruir TeamLedgers cuando ScoringAudit estuvo caído.
/// </summary>
public sealed record ReconcileSessionScoringCommand(Guid OperatorId, Guid SessionId) : IRequest<ReconcileSessionScoringResult>;

public sealed record ReconcileSessionScoringResult(int TeamsPublished, int EvidencesPublished);

public sealed class ReconcileSessionScoringHandler
    : IRequestHandler<ReconcileSessionScoringCommand, ReconcileSessionScoringResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IMissionIntegrationService _missionIntegration;
    private readonly IDomainEventPublisher _eventPublisher;

    public ReconcileSessionScoringHandler(
        ILiveSessionRepository sessionRepository,
        ITeamRepository teamRepository,
        IMissionIntegrationService missionIntegration,
        IDomainEventPublisher eventPublisher)
    {
        _sessionRepository = sessionRepository;
        _teamRepository = teamRepository;
        _missionIntegration = missionIntegration;
        _eventPublisher = eventPublisher;
    }

    public async Task<ReconcileSessionScoringResult> Handle(
        ReconcileSessionScoringCommand request,
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

        var teams = await _teamRepository.GetByIdsAsync(session.RegisteredTeamIds, cancellationToken);
        var teamNames = teams.ToDictionary(t => t.Id, t => t.Name);
        var events = new List<IDomainEvent>();

        if (session.StartedAtUtc.HasValue)
        {
            events.Add(new SessionStartedEvent
            {
                SessionId = session.Id,
                MissionRef = session.MissionRef,
                OperatorRef = session.OperatorRef,
                ParticipatingTeamIds = session.RegisteredTeamIds.ToList(),
                StartedAtUtc = session.StartedAtUtc.Value
            });
        }

        foreach (var teamId in session.RegisteredTeamIds)
        {
            events.Add(new TeamRegisteredEvent
            {
                SessionId = session.Id,
                TeamId = teamId,
                TeamName = teamNames.GetValueOrDefault(teamId, $"Team-{teamId:N}"[..12])
            });
        }

        var nodeById = session.AllowedNodes.ToDictionary(n => n.NodeId);
        var validEvidence = session.EvidenceSubmissions
            .Where(e => e.IsValid == true)
            .GroupBy(e => new { e.TeamId, e.MissionNodeId })
            .Select(g => g.OrderBy(e => e.SubmittedAtUtc).First())
            .ToList();

        foreach (var evidence in validEvidence)
        {
            if (!nodeById.TryGetValue(evidence.MissionNodeId, out var node))
                continue;

            var elapsed = session.StartedAtUtc.HasValue
                ? Math.Max(0, (evidence.SubmittedAtUtc - session.StartedAtUtc.Value).TotalSeconds)
                : 0;

            events.Add(new EvidenceValidatedEvent
            {
                SessionId = session.Id,
                EvidenceSubmissionId = evidence.Id,
                TeamId = evidence.TeamId,
                MissionNodeId = evidence.MissionNodeId,
                NodeType = node.NodeType,
                NodeTitle = node.Title,
                BaseScore = node.BaseScore,
                DifficultyMultiplier = session.DifficultyMultiplier,
                ElapsedSeconds = elapsed
            });
        }

        if (events.Count > 0)
            await _eventPublisher.PublishAsync(events, cancellationToken);

        return new ReconcileSessionScoringResult(
            TeamsPublished: session.RegisteredTeamIds.Count,
            EvidencesPublished: validEvidence.Count);
    }
}
