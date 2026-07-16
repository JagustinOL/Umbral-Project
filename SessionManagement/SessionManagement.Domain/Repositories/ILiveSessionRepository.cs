using SessionManagement.Domain.Aggregates;

namespace SessionManagement.Domain.Repositories;

/// <summary>
/// Contrato del repositorio para LiveSession.
///
/// GetByIdAsync debe cargar el agregado completo:
/// EvidenceSubmissions, ReleasedHints y AllowedNodes.
/// Sin ellos, las invariantes RB-03, RB-04 y RB-05 no pueden verificarse.
/// </summary>
public interface ILiveSessionRepository
{
    Task<LiveSession?> GetByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<LiveSession?> GetByJoinCodeAsync(
        string joinCode,
        CancellationToken cancellationToken = default);

    Task<LiveSession?> GetByIdForOperatorAsync(
        Guid sessionId,
        Guid operatorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LiveSession>> GetPendingByOperatorAsync(
        Guid operatorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LiveSession>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sesión abierta (Pending/Preparation) con solicitud de unión Pending del equipo.
    /// Permite que todos los miembros del equipo vean el estado de espera (RN-15).
    /// </summary>
    Task<Guid?> FindOpenSessionIdWithPendingJoinByTeamAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOpenSessionsByOperatorAsync(
        Guid operatorId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOpenSessionForMissionByOperatorAsync(
        Guid operatorId,
        Guid missionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LiveSession>> GetOpenSessionsByOperatorAsync(
        Guid operatorId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOpenSessionsByMissionAsync(
        Guid missionId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        LiveSession session,
        CancellationToken cancellationToken = default);
}