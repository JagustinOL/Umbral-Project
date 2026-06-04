namespace SessionManagement.Application.Dtos;

public sealed record OperatorOpenSessionDto(
    Guid SessionId,
    Guid MissionId,
    string JoinCode,
    string Status);
