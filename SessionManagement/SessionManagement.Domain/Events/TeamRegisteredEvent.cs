using SessionManagement.Domain.Common;

namespace SessionManagement.Domain.Events;

/// <summary>
/// Disparado cuando un equipo se registra exitosamente en una sesión.
///
/// Consumidores:
/// — ScoringAudit: crea el TeamLedger inicial para este equipo
///   en esta sesión (puntaje en cero, listo para recibir ScoreEntries).
/// </summary>
public sealed record TeamRegisteredEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;
    public string TeamCode { get; init; } = string.Empty;
}