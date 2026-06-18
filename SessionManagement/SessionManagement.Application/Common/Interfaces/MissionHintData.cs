namespace SessionManagement.Application.Common.Interfaces;

public sealed record MissionHintData(
    Guid Id,
    int Order,
    string Content,
    int PenaltyPoints);
