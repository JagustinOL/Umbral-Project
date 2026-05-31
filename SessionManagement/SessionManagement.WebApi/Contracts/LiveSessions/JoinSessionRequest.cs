namespace SessionManagement.WebApi.Contracts.LiveSessions;

public sealed record JoinSessionRequest(
    string JoinCode,
    Guid TeamId
);

