using Common;

namespace SessionManagement.Domain.Events;

/// <summary>
/// Disparado cuando el Operador (o el sistema) libera una pista a un equipo.
///
/// Consumidores:
/// — ScoringAudit: guarda el evento en el AuditLog (Flujo 5 del DDD).
///   El descuento de puntos por la pista se aplica aquí también como
///   un ScoreEntry negativo automático.
/// — SignalR Hub: entrega la pista en tiempo real al panel del equipo (RF-07).
/// </summary>
public sealed record HintReleasedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public Guid HintId { get; init; }
    public Guid MissionNodeId { get; init; }
    public int PenaltyPoints { get; init; }
    public bool WasManualRelease { get; init; }
}