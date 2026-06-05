namespace MissionManagement.WebApi.Contracts.Missions;

public sealed record CreateMissionRequest(
    string Title,
    string Description,
    int Difficulty,
    int? MaxDurationMinutes
);

