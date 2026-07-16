using MediatR;
using UserService.Application.Common;
using UserService.Application.Common.Interfaces;
using UserService.Application.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Auth.Queries.ValidateToken;

public sealed class ValidateTokenHandler : IRequestHandler<ValidateTokenQuery, ValidatedUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IAccessTokenReader _accessTokenReader;

    public ValidateTokenHandler(IUserRepository userRepository, IAccessTokenReader accessTokenReader)
    {
        _userRepository = userRepository;
        _accessTokenReader = accessTokenReader;
    }

    public async Task<ValidatedUserResult> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            throw new UnauthorizedException("Token de acceso requerido.");

        var (userId, _) = _accessTokenReader.Read(request.AccessToken);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            throw new UnauthorizedException("Usuario no registrado en UserService.");

        if (!user.IsActive)
            throw new UnauthorizedException("El usuario está inactivo.");

        var roleName = UserRoleNames.FromDomainRole(user.Role);
        var authorizedRoles = new[] { roleName };

        return new ValidatedUserResult(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            roleName,
            user.Status.ToString(),
            authorizedRoles);
    }
}
