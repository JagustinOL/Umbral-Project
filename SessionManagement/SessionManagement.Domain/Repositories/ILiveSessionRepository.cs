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

    /// <summary>
    /// Retorna todas las sesiones activas de un operador.
    /// Usado para la consulta del panel del Operador (RB-10).
    /// </summary>
    Task<IReadOnlyList<LiveSession>> GetActiveSessionsByOperatorAsync(
        Guid operatorRef,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        LiveSession session,
        CancellationToken cancellationToken = default);
}