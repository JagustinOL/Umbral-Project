using MediatR;

namespace UserService.Application.Auth.Queries.ValidateToken;

public sealed record ValidateTokenQuery(string AccessToken) : IRequest<ValidatedUserResult>;

public sealed record ValidatedUserResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Status,
    IReadOnlyList<string> Roles);
