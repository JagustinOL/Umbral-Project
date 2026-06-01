namespace SessionManagement.WebApi.Contracts.Teams;

public sealed record CreateTeamRequest(
    string Name,
    Guid CreatorId,
    string? CreatorDisplayName
);
