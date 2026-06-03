namespace SessionManagement.WebApi.Contracts.LiveSessions;

public sealed record SubmitTreasureHuntCodeRequest(
    Guid NodeId,
    string FoundCode
);

