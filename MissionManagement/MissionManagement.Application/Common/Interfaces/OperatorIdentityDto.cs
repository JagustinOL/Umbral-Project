namespace MissionManagement.Application.Common.Interfaces;

public sealed record OperatorIdentityDto(
    Guid OperatorId,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive
);

