namespace SessionManagement.WebApi.Contracts.Teams;

public sealed record ProcessJoinRequestRequest(
    bool Approve,
    Guid RequestorId
);
