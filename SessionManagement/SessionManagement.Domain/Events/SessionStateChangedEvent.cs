using Common;
using SessionManagement.Domain.Aggregates;

namespace SessionManagement.Domain.Events;

/// <summary>
/// Disparado en cada cambio de estado de la sesión
/// (Paused, Resumed, Cancelled).
///
/// Consumidores:
/// — ScoringAudit: guarda el evento en el AuditLog para trazabilidad.
/// — SignalR Hub: notifica en tiempo real al panel del Operador
///   y a los tableros de los equipos (RF-13).
/// </summary>
public sealed record SessionStateChangedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public LiveSessionStatus PreviousStatus { get; init; }
    public LiveSessionStatus NewStatus { get; init; }
    public string? Reason { get; init; }
}