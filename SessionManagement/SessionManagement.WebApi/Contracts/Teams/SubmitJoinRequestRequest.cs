namespace SessionManagement.WebApi.Contracts.Teams;

public sealed record SubmitJoinRequestRequest(
    string TeamCode,
    Guid PlayerRef,
    string DisplayName
);
