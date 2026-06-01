namespace SessionManagement.Application.Dtos;

public sealed record CreatedTeamDto(
    Guid TeamId,
    string Name,
    string Code
);
