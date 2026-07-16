namespace UserService.Application.Common.Interfaces;

public sealed record PlayerIdentityDto(
    Guid PlayerId,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive
);
