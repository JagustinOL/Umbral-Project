namespace SessionManagement.Application.Dtos;

public sealed record JoinSessionResultDto(
    Guid SessionId,
    string Status,
    Guid? RequestId);
