namespace SessionManagement.Application.Common.Interfaces;

public sealed record MissionNodeValidationData(
    Guid NodeId,
    string NodeType,
    int ExecutionOrder,
    int BaseScore,
    string ExpectedValue
);

