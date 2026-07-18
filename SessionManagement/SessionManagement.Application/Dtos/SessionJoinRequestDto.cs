namespace SessionManagement.Application.Dtos;

public sealed record SessionJoinRequestDto(
    Guid RequestId,
    Guid TeamId,
    string? TeamName,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? ResolvedAtUtc);
