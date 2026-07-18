using SessionManagement.Application.Common;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Evidence.Processing;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Common;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.Facades;

/// <summary>
/// Patrón Facade — coordina operaciones de sesión, persistencia y publicación de eventos.
/// </summary>
public interface ISessionOperationFacade
{
    Task<CreatedLiveSessionDto> CreateSessionAsync(
        Guid operatorId,
        Guid missionId,
        CancellationToken cancellationToken = default);

    Task StartSessionAsync(Guid operatorId, Guid sessionId, CancellationToken cancellationToken = default);
    Task FinalizeSessionAsync(Guid operatorId, Guid sessionId, CancellationToken cancellationToken = default);
    Task CancelSessionAsync(Guid operatorId, Guid sessionId, CancellationToken cancellationToken = default);

    Task<SubmissionResultDto> SubmitTriviaAsync(
        Guid sessionId,
        Guid teamId,
        Guid nodeId,
        string answer,
        int questionIndex,
        CancellationToken cancellationToken = default);

    Task<SubmissionResultDto> SubmitTreasureHuntAsync(
        Guid sessionId,
        Guid teamId,
        Guid nodeId,
        string foundCode,
        CancellationToken cancellationToken = default);

    Task SaveAndPublishAsync(LiveSession session, CancellationToken cancellationToken = default);
}

public sealed class SessionOperationFacade : ISessionOperationFacade
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IMissionIntegrationService _missionIntegration;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILiveSessionRealtimeNotifier _realtimeNotifier;
    private readonly TriviaEvidenceSubmissionProcessor _triviaProcessor;
    private readonly TreasureHuntEvidenceSubmissionProcessor _treasureHuntProcessor;

    public SessionOperationFacade(
        ILiveSessionRepository sessionRepository,
        ITeamRepository teamRepository,
        IMissionIntegrationService missionIntegration,
        IDomainEventPublisher eventPublisher,
        ILiveSessionRealtimeNotifier realtimeNotifier,
        TriviaEvidenceSubmissionProcessor triviaProcessor,
        TreasureHuntEvidenceSubmissionProcessor treasureHuntProcessor)
    {
        _sessionRepository = sessionRepository;
        _teamRepository = teamRepository;
        _missionIntegration = missionIntegration;
        _eventPublisher = eventPublisher;
        _realtimeNotifier = realtimeNotifier;
        _triviaProcessor = triviaProcessor;
        _treasureHuntProcessor = treasureHuntProcessor;
    }

    public async Task<CreatedLiveSessionDto> CreateSessionAsync(
        Guid operatorId,
        Guid missionId,
        CancellationToken cancellationToken = default)
    {
        var assigned = await _missionIntegration.GetAssignedMissionsForOperatorAsync(operatorId, cancellationToken);
        var mission = assigned.FirstOrDefault(x => x.MissionId == missionId);
        if (mission is null)
            throw new NotFoundException("La misión no está asignada al operador.");

        if (!string.Equals(await _missionIntegration.GetMissionStatusAsync(missionId, cancellationToken), "Active", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Solo se pueden crear sesiones para misiones en estado Active.");

        var nodeData = await _missionIntegration.GetNodeValidationDataAsync(missionId, cancellationToken);
        var difficultyMultiplier = await _missionIntegration.GetMissionDifficultyMultiplierAsync(missionId, cancellationToken);
        var allowedNodes = nodeData
            .OrderBy(x => x.ExecutionOrder)
            .Select(x => new AllowedNode(x.NodeId, x.NodeType, x.BaseScore, x.Title))
            .ToList();

        var session = LiveSession.CreateForMission(
            missionRef: missionId,
            operatorRef: operatorId,
            allowedNodes: allowedNodes,
            difficultyMultiplier: difficultyMultiplier);

        await SaveAndPublishAsync(session, cancellationToken);

        return new CreatedLiveSessionDto(session.Id, session.JoinCode);
    }

    public async Task StartSessionAsync(Guid operatorId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetOperatorSessionAsync(operatorId, sessionId, cancellationToken);
        await ValidateMissionAssignmentAsync(operatorId, session.MissionRef, cancellationToken);

        if (session.Status == LiveSessionStatus.Pending)
            session.BeginPreparation();

        try
        {
            session.StartSession();
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await SaveAndPublishAsync(session, cancellationToken);
        await TeamSessionLockService.LockTeamsForSessionAsync(session, _teamRepository, cancellationToken);
    }

    public async Task FinalizeSessionAsync(Guid operatorId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetOperatorSessionAsync(operatorId, sessionId, cancellationToken);
        await ValidateMissionAssignmentAsync(operatorId, session.MissionRef, cancellationToken);

        // Idempotente: si ya está cerrada, no reintentamos la transición.
        if (session.Status is LiveSessionStatus.Finalized or LiveSessionStatus.Cancelled)
        {
            await TeamSessionLockService.ReleaseTeamsFromSessionAsync(session, _teamRepository, cancellationToken);
            return;
        }

        try
        {
            session.Finalize();
        }
        catch (SessionManagement.Domain.Exceptions.SessionDomainException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await SaveAndPublishAsync(session, cancellationToken);
        await TeamSessionLockService.ReleaseTeamsFromSessionAsync(session, _teamRepository, cancellationToken);
    }

    public async Task CancelSessionAsync(Guid operatorId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetOperatorSessionAsync(operatorId, sessionId, cancellationToken);
        await ValidateMissionAssignmentAsync(operatorId, session.MissionRef, cancellationToken);

        try
        {
            session.Cancel("El operador canceló la sesión antes de iniciarla.");
        }
        catch (SessionManagement.Domain.Exceptions.SessionDomainException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await SaveAndPublishAsync(session, cancellationToken);
        await TeamSessionLockService.ReleaseTeamsFromSessionAsync(session, _teamRepository, cancellationToken);
    }

    public async Task<SubmissionResultDto> SubmitTriviaAsync(
        Guid sessionId,
        Guid teamId,
        Guid nodeId,
        string answer,
        int questionIndex,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        var rules = await BuildRulesAsync(session.MissionRef, cancellationToken);
        var result = _triviaProcessor.Process(
            session,
            new EvidenceSubmissionRequest(teamId, nodeId, answer, rules, questionIndex));
        await SaveAndPublishAsync(session, cancellationToken);
        await ReleaseTeamsIfSessionFinalizedAsync(session, cancellationToken);
        await _realtimeNotifier.NotifyTriviaAnswerSubmittedAsync(
            sessionId, teamId, nodeId, result.IsCorrect, result.NodeCompleted, result.AwardedPoints, cancellationToken);
        await _realtimeNotifier.NotifyTeamProgressUpdatedAsync(
            sessionId, teamId, result.CurrentNodeId, result.NextNodeId, result.NodeCompleted, cancellationToken);
        return MapResult(result);
    }

    public async Task<SubmissionResultDto> SubmitTreasureHuntAsync(
        Guid sessionId,
        Guid teamId,
        Guid nodeId,
        string foundCode,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        var rules = await BuildRulesAsync(session.MissionRef, cancellationToken);
        var result = _treasureHuntProcessor.Process(
            session,
            new EvidenceSubmissionRequest(teamId, nodeId, foundCode, rules));

        if (result.IsCorrect && result.NodeCompleted)
            await ReleaseRemainingTreasureHintsAsync(session, teamId, nodeId, cancellationToken);

        await SaveAndPublishAsync(session, cancellationToken);
        await ReleaseTeamsIfSessionFinalizedAsync(session, cancellationToken);
        await _realtimeNotifier.NotifyHuntLocationReachedAsync(
            sessionId, teamId, nodeId, result.IsCorrect, cancellationToken);
        await _realtimeNotifier.NotifyTeamProgressUpdatedAsync(
            sessionId, teamId, result.CurrentNodeId, result.NextNodeId, result.NodeCompleted, cancellationToken);
        return MapResult(result);
    }

    private async Task ReleaseRemainingTreasureHintsAsync(
        LiveSession session,
        Guid teamId,
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var catalogHints = await _missionIntegration.GetHintsForNodeAsync(
            session.MissionRef, nodeId, cancellationToken);
        if (catalogHints.Count == 0)
            return;

        session.ReleaseRemainingHintsAutomatically(
            teamId,
            nodeId,
            catalogHints.Select(h => (h.Id, h.Order)).ToList());
    }

    private async Task ReleaseTeamsIfSessionFinalizedAsync(
        LiveSession session,
        CancellationToken cancellationToken)
    {
        if (session.Status is not (LiveSessionStatus.Finalized or LiveSessionStatus.Cancelled))
            return;

        await TeamSessionLockService.ReleaseTeamsFromSessionAsync(session, _teamRepository, cancellationToken);
    }

    public async Task SaveAndPublishAsync(LiveSession session, CancellationToken cancellationToken = default)
    {
        await _sessionRepository.SaveAsync(session, cancellationToken);
        var events = session.DomainEvents.ToList();
        if (events.Count > 0)
        {
            await _eventPublisher.PublishAsync(events, cancellationToken);
            session.ClearDomainEvents();
        }
    }

    private async Task<LiveSession> GetOperatorSessionAsync(
        Guid operatorId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdForOperatorAsync(sessionId, operatorId, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró la sesión {sessionId} para el operador {operatorId}.");
        return session;
    }

    private async Task<LiveSession> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró la sesión con Id={sessionId}.");
        return session;
    }

    private async Task ValidateMissionAssignmentAsync(
        Guid operatorId,
        Guid missionId,
        CancellationToken cancellationToken)
    {
        var assigned = await _missionIntegration.GetAssignedMissionsForOperatorAsync(operatorId, cancellationToken);
        if (!assigned.Any(x => x.MissionId == missionId))
            throw new NotFoundException("La misión de la sesión no está asignada al operador.");
    }

    private async Task<IReadOnlyList<NodeValidationRule>> BuildRulesAsync(
        Guid missionId,
        CancellationToken cancellationToken)
    {
        var data = await _missionIntegration.GetNodeValidationDataAsync(missionId, cancellationToken);
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

    private static SubmissionResultDto MapResult(SubmissionResult result) =>
        new(
            result.IsCorrect,
            result.CurrentNodeId,
            result.NextNodeId,
            result.AwardedPoints,
            result.AnsweredQuestionIndex,
            result.TotalQuestions,
            result.NodeCompleted);
}
