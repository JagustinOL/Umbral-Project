namespace SessionManagement.Application.Common.Interfaces;

public interface ILiveSessionRealtimeNotifier
{
    Task NotifySessionStateChangedAsync(
        Guid sessionId,
        string previousStatus,
        string newStatus,
        string? reason,
        CancellationToken cancellationToken = default);

    Task NotifyManualPenaltyAsync(
        Guid sessionId,
        Guid teamId,
        int penaltyPoints,
        string reason,
        CancellationToken cancellationToken = default);

    Task NotifyHintReleasedAsync(
        Guid sessionId,
        Guid teamId,
        Guid hintId,
        Guid missionNodeId,
        int penaltyPoints,
        CancellationToken cancellationToken = default);

    Task NotifySupportMessageAsync(
        Guid sessionId,
        Guid teamId,
        string message,
        CancellationToken cancellationToken = default);

    Task NotifyJoinRequestReceivedAsync(
        Guid sessionId,
        Guid teamId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task NotifyJoinRequestResolvedAsync(
        Guid sessionId,
        Guid teamId,
        Guid requestId,
        string decision,
        CancellationToken cancellationToken = default);

    Task NotifyTeamProgressUpdatedAsync(
        Guid sessionId,
        Guid teamId,
        Guid? currentNodeId,
        Guid? nextNodeId,
        bool nodeCompleted,
        CancellationToken cancellationToken = default);

    Task NotifyTriviaAnswerSubmittedAsync(
        Guid sessionId,
        Guid teamId,
        Guid nodeId,
        bool isCorrect,
        CancellationToken cancellationToken = default);

    Task NotifyHuntLocationReachedAsync(
        Guid sessionId,
        Guid teamId,
        Guid nodeId,
        bool isCorrect,
        CancellationToken cancellationToken = default);

    Task NotifyScoreUpdateAsync(
        Guid sessionId,
        Guid teamId,
        int newTotalScore,
        object? ranking,
        CancellationToken cancellationToken = default);

    Task NotifyTeamDisconnectedAsync(
        Guid sessionId,
        Guid teamId,
        CancellationToken cancellationToken = default);
}
