using SessionManagement.Domain.Aggregates;

namespace SessionManagement.Domain.Repositories;

/// <summary>
/// Contrato del repositorio para Team.
///
/// GetBySessionIdAsync carga TODOS los equipos de una sesión.
/// Necesario para que el Application Service pueda bloquear/desbloquear
/// en masa al reaccionar a SessionStartedEvent y SessionFinalizedEvent.
/// </summary>
public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);

    Task<Team?> GetByCodeAsync(
        string teamCode,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string teamName,
        Guid? excludingTeamId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(
        string teamCode,
        Guid? excludingTeamId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Team>> GetBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Team team,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste múltiples equipos en una sola operación.
    /// Útil para el bloqueo/desbloqueo masivo al iniciar/finalizar sesión.
    /// </summary>
    Task SaveRangeAsync(
        IEnumerable<Team> teams,
        CancellationToken cancellationToken = default);
}