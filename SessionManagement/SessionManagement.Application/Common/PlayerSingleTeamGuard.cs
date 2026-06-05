using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Common;

/// <summary>
/// Un jugador solo puede pertenecer a un equipo activo a la vez
/// y no puede tener solicitudes pendientes en otro equipo.
/// </summary>
internal static class PlayerSingleTeamGuard
{
    public static async Task EnsureCanJoinOrCreateTeamAsync(
        ITeamRepository teamRepository,
        Guid playerId,
        Guid? targetTeamId = null,
        CancellationToken cancellationToken = default)
    {
        var activeTeam = await teamRepository.GetActiveTeamByPlayerRefAsync(
            playerId,
            cancellationToken);

        if (activeTeam is not null && (targetTeamId is null || activeTeam.Id != targetTeamId))
        {
            throw new ConflictException(
                "El jugador ya pertenece a un equipo activo. Debe abandonar o disolver su equipo actual antes de unirse a otro.");
        }

        var pendingTeam = await teamRepository.GetActiveTeamWithPendingJoinRequestAsync(
            playerId,
            cancellationToken);

        if (pendingTeam is not null && (targetTeamId is null || pendingTeam.Id != targetTeamId))
        {
            throw new ConflictException(
                "El jugador ya tiene una solicitud de unión pendiente en otro equipo.");
        }
    }
}
