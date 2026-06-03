namespace SessionManagement.WebApi.Contracts.Teams;

public sealed record UpdateTeamRequest(
    string NewName,
    Guid RequestorId
);
