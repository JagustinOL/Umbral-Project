using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;

namespace ScoringAudit.Domain.Repositories;

/// <summary>
/// Contrato del repositorio para AuditLog.
///
/// AddEventAsync: operación de escritura más frecuente del contexto —
/// ocurre en cada evento de dominio de SessionManagement.
///
/// GetHistoryBySessionAsync: operación de lectura para el panel
/// de auditoría del Operador (RF-15). Retorna el historial completo
/// ordenado cronológicamente.
/// </summary>
public interface IAuditLogRepository
{
    Task<AuditLog?> GetBySessionAsync(
        Guid sessionRef,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna el historial de eventos paginado para el panel de auditoría.
    /// </summary>
    Task<IReadOnlyList<SessionEvent>> GetHistoryBySessionAsync(
        Guid sessionRef,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default);
}