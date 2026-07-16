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

    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetClosedPaginatedAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? operatorRef,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default);
}