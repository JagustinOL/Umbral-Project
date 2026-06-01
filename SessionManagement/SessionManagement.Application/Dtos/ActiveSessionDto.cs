namespace SessionManagement.Application.Dtos;

public sealed record ActiveSessionDto(
    Guid SessionId,
    Guid MissionRef,
    string JoinCode,
    string Status,
    DateTime CreatedAtUtc
);

