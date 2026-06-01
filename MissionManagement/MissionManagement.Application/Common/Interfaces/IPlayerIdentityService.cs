namespace MissionManagement.Application.Common.Interfaces;

public interface IPlayerIdentityService
{
    Task<Guid> CreatePlayerAsync(
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlayerIdentityDto>> GetPlayersAsync(
        CancellationToken cancellationToken = default);

    Task<PlayerIdentityDto> GetPlayerByIdAsync(
        Guid playerId,
        CancellationToken cancellationToken = default);

    Task UpdatePlayerAsync(
        Guid playerId,
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default);

    Task DeactivatePlayerAsync(
        Guid playerId,
        CancellationToken cancellationToken = default);
}
