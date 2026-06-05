namespace SessionManagement.Application.Dtos;

public sealed record JoinRequestDto(
    Guid RequestId,
    Guid PlayerRef,
    string DisplayName,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? ReviewedAtUtc
);
