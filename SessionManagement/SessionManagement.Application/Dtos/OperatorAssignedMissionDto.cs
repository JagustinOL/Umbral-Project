namespace SessionManagement.Application.Dtos;

public sealed record OperatorAssignedMissionDto(
    Guid MissionId,
    Guid OperatorId,
    string Title
);

