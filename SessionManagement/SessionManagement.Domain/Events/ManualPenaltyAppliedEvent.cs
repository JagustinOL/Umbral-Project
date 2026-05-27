using Common;

namespace SessionManagement.Domain.Events;

/// <summary>
/// Disparado cuando el Operador aplica una penalización manual a un equipo.
/// Flujo 4 del DDD — ruta corregida: va directo a ScoringAudit.
///
/// RB-06: Toda penalización debe registrar motivo y momento de aplicación.
/// El motivo viaja en este evento para que ScoringAudit lo persista
/// en el ScoreEntry como PenaltyReason (Value Object inmutable).
///
/// Consumidores:
/// — ScoringAudit: registra un ScoreEntry negativo inmutable en TeamLedger.
/// — SignalR Hub: notifica al panel del Operador la confirmación.
/// </summary>
public sealed record ManualPenaltyAppliedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public Guid OperatorRef { get; init; }
    public int PenaltyPoints { get; init; }

    /// <summary>
    /// Motivo obligatorio de la penalización (RB-06).
    /// Se convierte en PenaltyReason Value Object en ScoringAudit.
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}