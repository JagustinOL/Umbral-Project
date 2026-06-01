namespace SessionManagement.Application.Dtos;

public sealed record TeamMemberDto(
    Guid PlayerRef,
    string DisplayName,
    string Role,
    DateTime JoinedAtUtc
);
