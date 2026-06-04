namespace SessionManagement.Application.Dtos;

public sealed record PlayerTeamMembershipDto(
    bool IsMember,
    Guid? TeamId,
    string? TeamName,
    string? TeamCode,
    string? Role,
    bool HasPendingJoinRequest,
    Guid? PendingTeamId
);
