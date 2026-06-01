namespace SessionManagement.WebApi.Contracts.OperatorSessions;

public sealed record CreateLiveSessionRequest(
    Guid MissionId
);

