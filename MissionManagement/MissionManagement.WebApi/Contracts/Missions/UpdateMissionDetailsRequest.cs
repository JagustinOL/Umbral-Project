namespace MissionManagement.WebApi.Contracts.Missions;

public sealed record UpdateMissionDetailsRequest(
    string Title,
    string Description,
    int? MaxDurationMinutes
);

