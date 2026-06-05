namespace SessionManagement.Application.Common.Interfaces;

public sealed record AssignedMissionData(
    Guid MissionId,
    Guid OperatorId,
    string Title
);

