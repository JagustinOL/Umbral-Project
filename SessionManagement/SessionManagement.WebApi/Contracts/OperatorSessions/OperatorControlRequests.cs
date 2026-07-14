namespace SessionManagement.WebApi.Contracts.OperatorSessions;

public sealed record ReleaseManualHintRequest(Guid HintId);

public sealed record ApplyManualPenaltyRequest(int Points, string Reason);

public sealed record SendSupportMessageRequest(string Message);

public sealed record ToggleSessionPauseRequest(string? Reason);
