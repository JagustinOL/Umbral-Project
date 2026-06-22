using UserService.Application.Common.Interfaces;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;
using UserService.Domain.Repositories;
using UserService.Infrastructure.External.Keycloak;

namespace UserService.Infrastructure.Sync;

public sealed class UserSyncPlayerIdentityService : IPlayerIdentityService
{
    private readonly KeycloakPlayerIdentityService _inner;
    private readonly IUserRepository _userRepository;

    public UserSyncPlayerIdentityService(
        KeycloakPlayerIdentityService inner,
        IUserRepository userRepository)
    {
        _inner = inner;
        _userRepository = userRepository;
    }

    public async Task<Guid> CreatePlayerAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var playerId = await _inner.CreatePlayerAsync(firstName, lastName, email, password, cancellationToken);

        await _userRepository.SaveAsync(
            User.Create(playerId, email, firstName, lastName, UserRole.Player, UserStatus.Active),
            cancellationToken);

        return playerId;
    }

    public Task<IReadOnlyList<PlayerIdentityDto>> GetPlayersAsync(CancellationToken cancellationToken = default) =>
        _inner.GetPlayersAsync(cancellationToken);

    public Task<PlayerIdentityDto> GetPlayerByIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _inner.GetPlayerByIdAsync(playerId, cancellationToken);

    public async Task UpdatePlayerAsync(
        Guid playerId,
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default)
    {
        await _inner.UpdatePlayerAsync(playerId, firstName, lastName, email, cancellationToken);

        var user = await _userRepository.GetByIdAsync(playerId, cancellationToken);
        if (user is not null)
        {
            user.UpdateProfile(firstName, lastName, email);
            await _userRepository.SaveAsync(user, cancellationToken);
        }
    }

    public async Task DeactivatePlayerAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        await _inner.DeactivatePlayerAsync(playerId, cancellationToken);

        var user = await _userRepository.GetByIdAsync(playerId, cancellationToken);
        if (user is not null)
        {
            user.Deactivate();
            await _userRepository.SaveAsync(user, cancellationToken);
        }
    }
}
