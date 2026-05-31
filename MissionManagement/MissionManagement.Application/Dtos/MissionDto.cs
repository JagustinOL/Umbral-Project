namespace MissionManagement.Application.Dtos;

public sealed record MissionDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Difficulty,
    int? MaxDurationMinutes,
    DateTime CreatedAtUtc,
    DateTime? LastModifiedAtUtc,
    IReadOnlyList<Guid> OperatorIds
);

