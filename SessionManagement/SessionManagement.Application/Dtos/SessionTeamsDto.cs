namespace SessionManagement.Application.Dtos;

public sealed record SessionTeamsDto(
    Guid SessionId,
    IReadOnlyList<Guid> TeamIds,
    int TeamCount
);

