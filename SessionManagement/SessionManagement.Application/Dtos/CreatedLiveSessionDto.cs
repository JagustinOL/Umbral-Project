namespace SessionManagement.Application.Dtos;

public sealed record CreatedLiveSessionDto(
    Guid SessionId,
    string JoinCode
);

