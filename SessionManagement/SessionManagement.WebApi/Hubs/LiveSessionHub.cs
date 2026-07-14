using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SessionManagement.WebApi.Hubs;

[Authorize]
public sealed class LiveSessionHub : Hub<ILiveSessionClient>
{
    public static string SessionGroup(Guid sessionId) => $"Session_{sessionId}";
    public static string OperatorGroup(Guid sessionId) => $"Operator_Session_{sessionId}";
    public static string TeamGroup(Guid sessionId, Guid teamId) => $"Session_{sessionId}_Team_{teamId}";

    public async Task JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
    }

    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
    }

    public async Task JoinOperatorSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, OperatorGroup(sessionId));
        await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
    }

    public async Task LeaveOperatorSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, OperatorGroup(sessionId));
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
    }

    public async Task JoinTeamSession(Guid sessionId, Guid teamId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
        await Groups.AddToGroupAsync(Context.ConnectionId, TeamGroup(sessionId, teamId));
    }
}
