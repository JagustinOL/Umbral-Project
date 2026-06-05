namespace MissionManagement.Application.Dtos;

public sealed record MissionDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Difficulty,
    decimal DifficultyScoreMultiplier,
    int? MaxDurationMinutes,
    DateTime CreatedAtUtc,
    DateTime? LastModifiedAtUtc,
    IReadOnlyList<Guid> OperatorIds
);

