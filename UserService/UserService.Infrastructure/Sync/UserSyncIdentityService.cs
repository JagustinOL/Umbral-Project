using UserService.Application.Common;
using UserService.Application.Common.Interfaces;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;
using UserService.Domain.Repositories;
using UserService.Infrastructure.External.Keycloak;

namespace UserService.Infrastructure.Sync;

public sealed class UserSyncIdentityService : IIdentityService
{
    private readonly KeycloakIdentityService _inner;
    private readonly IUserRepository _userRepository;

    public UserSyncIdentityService(KeycloakIdentityService inner, IUserRepository userRepository)
    {
        _inner = inner;
        _userRepository = userRepository;
    }

    public async Task<CreateOperatorResult> CreateOperatorAsync(
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.CreateOperatorAsync(firstName, lastName, email, cancellationToken);

        await _userRepository.SaveAsync(
            User.Create(
                result.OperatorId,
                email,
                firstName,
                lastName,
                UserRole.Operator,
                UserStatus.Inactive),
            cancellationToken);

        return result;
    }

    public async Task SetupOperatorPasswordAsync(
        string email,
        string setupCode,
        string password,
        CancellationToken cancellationToken = default)
    {
        await _inner.SetupOperatorPasswordAsync(email, setupCode, password, cancellationToken);

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is not null)
        {
            user.Activate();
            await _userRepository.SaveAsync(user, cancellationToken);
        }
    }

    public async Task<Guid> CreateAdminAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var adminId = await _inner.CreateAdminAsync(firstName, lastName, email, password, cancellationToken);

        await _userRepository.SaveAsync(
            User.Create(adminId, email, firstName, lastName, UserRole.Admin, UserStatus.Active),
            cancellationToken);

        return adminId;
    }

    public Task<IReadOnlyList<OperatorIdentityDto>> GetOperatorsAsync(CancellationToken cancellationToken = default) =>
        _inner.GetOperatorsAsync(cancellationToken);

    public async Task DeactivateOperatorAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        await _inner.DeactivateOperatorAsync(operatorId, cancellationToken);

        var user = await _userRepository.GetByIdAsync(operatorId, cancellationToken);
        if (user is not null)
        {
            user.Deactivate();
            await _userRepository.SaveAsync(user, cancellationToken);
        }
    }
}
