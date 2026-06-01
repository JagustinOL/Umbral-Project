using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;

namespace MissionManagement.Infrastructure.External.Fakes;

public sealed class FakePlayerIdentityService : IPlayerIdentityService
{
    private static readonly Dictionary<Guid, PlayerIdentityDto> Players = [];
    private static readonly Lock Sync = new();

    public Task<Guid> CreatePlayerAsync(string firstName, string lastName, string email, CancellationToken cancellationToken = default)
    {
        var player = new PlayerIdentityDto(
            PlayerId: Guid.NewGuid(),
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            IsActive: true);

        lock (Sync)
        {
            if (Players.Values.Any(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                throw new ConflictException($"Ya existe un jugador registrado con el correo '{email}'.");

            Players[player.PlayerId] = player;
        }

        return Task.FromResult(player.PlayerId);
    }

    public Task<IReadOnlyList<PlayerIdentityDto>> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            IReadOnlyList<PlayerIdentityDto> result = Players.Values.ToList();
            return Task.FromResult(result);
        }
    }

    public Task<PlayerIdentityDto> GetPlayerByIdAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            if (!Players.TryGetValue(playerId, out var player))
                throw new NotFoundException($"No existe un jugador con Id={playerId}.");

            return Task.FromResult(player);
        }
    }

    public Task UpdatePlayerAsync(
        Guid playerId,
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            if (!Players.TryGetValue(playerId, out var existing))
                throw new NotFoundException($"No existe un jugador con Id={playerId}.");

            var duplicateEmail = Players.Values
                .Any(x => x.PlayerId != playerId && x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (duplicateEmail)
                throw new ConflictException($"Ya existe un jugador registrado con el correo '{email}'.");

            Players[playerId] = existing with
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email
            };
        }

        return Task.CompletedTask;
    }

    public Task DeactivatePlayerAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        lock (Sync)
        {
            if (!Players.TryGetValue(playerId, out var existing))
                throw new NotFoundException($"No existe un jugador con Id={playerId}.");

            Players[playerId] = existing with { IsActive = false };
        }

        return Task.CompletedTask;
    }
}
