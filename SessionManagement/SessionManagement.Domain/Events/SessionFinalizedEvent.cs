using SessionManagement.Domain.Common;

namespace SessionManagement.Domain.Events;

/// <summary>
/// Disparado cuando la sesión concluye (estado Finalized).
/// Evento con semántica propia — más específico que SessionStateChanged.
///
/// Consumidores:
/// — ScoringAudit: cierra el AuditLog, congela el TeamLedger y
///   genera el ranking final inmutable de la sesión.
/// — Team aggregate: desbloquea los equipos (IsLocked = false).
/// — SignalR Hub: notifica el cierre a todos los participantes.
/// </summary>
public sealed record SessionFinalizedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public DateTime FinalizedAtUtc { get; init; }
    public IReadOnlyList<Guid> ParticipatingTeamIds { get; init; } = [];
}