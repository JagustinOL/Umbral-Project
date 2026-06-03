namespace SessionManagement.Application.Dtos;

public sealed record TeamDetailsDto(
    Guid TeamId,
    string Name,
    string TeamCode,
    bool IsLocked,
    bool IsDisbanded,
    IReadOnlyList<TeamMemberDto> Members
);
