using Microsoft.AspNetCore.SignalR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.WebApi.Hubs;

namespace SessionManagement.WebApi.Realtime;

public sealed class SignalRLiveSessionRealtimeNotifier : ILiveSessionRealtimeNotifier
{
    private readonly IHubContext<LiveSessionHub, ILiveSessionClient> _hub;

    public SignalRLiveSessionRealtimeNotifier(IHubContext<LiveSessionHub, ILiveSessionClient> hub)
    {
        _hub = hub;
    }

    public Task NotifySessionStateChangedAsync(
        Guid sessionId, string previousStatus, string newStatus, string? reason,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(LiveSessionHub.SessionGroup(sessionId)).ReceiveSessionStateChanged(new
        {
            sessionId,
            previousStatus,
            newStatus,
            reason
        });

    public Task NotifyManualPenaltyAsync(
        Guid sessionId, Guid teamId, int penaltyPoints, string reason,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(LiveSessionHub.SessionGroup(sessionId)).ReceiveManualPenalty(new
        {
            sessionId,
            teamId,
            penaltyPoints,
            reason
        });

    public Task NotifyHintReleasedAsync(
        Guid sessionId, Guid teamId, Guid hintId, Guid missionNodeId, int penaltyPoints,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(LiveSessionHub.TeamGroup(sessionId, teamId)).ReceiveHintReleased(new
        {
            sessionId,
            teamId,
            hintId,
            missionNodeId,
            penaltyPoints
        });

    public Task NotifySupportMessageAsync(
        Guid sessionId, Guid teamId, string message,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(LiveSessionHub.TeamGroup(sessionId, teamId)).ReceiveSupportMessage(new
        {
            sessionId,
            teamId,
            message
        });

    public Task NotifyJoinRequestReceivedAsync(
        Guid sessionId, Guid teamId, Guid requestId,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(LiveSessionHub.OperatorGroup(sessionId)).JoinRequestReceived(new
        {
            sessionId,
            teamId,
            requestId
        });

    public Task NotifyJoinRequestResolvedAsync(
        Guid sessionId, Guid teamId, Guid requestId, string decision,
        CancellationToken cancellationToken = default) =>
        Task.WhenAll(
            _hub.Clients.Group(LiveSessionHub.OperatorGroup(sessionId)).JoinRequestResolved(new
            {
                sessionId,
                teamId,
                requestId,
                decision
            }),
            _hub.Clients.Group(LiveSessionHub.SessionGroup(sessionId)).JoinRequestResolved(new
            {
                sessionId,
                teamId,
                requestId,
                decision
            }));

    public Task NotifyTeamProgressUpdatedAsync(
        Guid sessionId, Guid teamId, Guid? currentNodeId, Guid? nextNodeId, bool nodeCompleted,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            sessionId,
            teamId,
            currentNodeId,
            nextNodeId,
            nodeCompleted
        };

        return Task.WhenAll(
            _hub.Clients.Group(LiveSessionHub.OperatorGroup(sessionId)).TeamProgressUpdated(payload),
            _hub.Clients.Group(LiveSessionHub.TeamGroup(sessionId, teamId)).TeamProgressUpdated(payload));
    }
    public Task NotifyTriviaAnswerSubmittedAsync(
        Guid sessionId, Guid teamId, Guid nodeId, bool isCorrect,
        bool nodeCompleted, int awardedPoints,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            sessionId,
            teamId,
            nodeId,
            isCorrect,
            nodeCompleted,
            awardedPoints
        };

        return Task.WhenAll(
            _hub.Clients.Group(LiveSessionHub.OperatorGroup(sessionId)).TriviaAnswerSubmitted(payload),
            _hub.Clients.Group(LiveSessionHub.TeamGroup(sessionId, teamId)).TriviaAnswerSubmitted(payload));
    }

    public Task NotifyHuntLocationReachedAsync(
        Guid sessionId, Guid teamId, Guid nodeId, bool isCorrect,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(LiveSessionHub.OperatorGroup(sessionId)).HuntLocationReached(new
        {
            sessionId,
            teamId,
            nodeId,
            isCorrect
        });

    public Task NotifyScoreUpdateAsync(
        Guid sessionId, Guid teamId, int newTotalScore, object? ranking,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(LiveSessionHub.SessionGroup(sessionId)).ReceiveScoreUpdate(new
        {
            sessionId,
            teamId,
            newTotalScore,
            ranking
        });

    public Task NotifyTeamDisconnectedAsync(
        Guid sessionId, Guid teamId,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(LiveSessionHub.OperatorGroup(sessionId)).TeamDisconnected(new
        {
            sessionId,
            teamId
        });
}
