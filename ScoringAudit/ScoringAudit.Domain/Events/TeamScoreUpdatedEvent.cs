using Common;
using ScoringAudit.Domain.ValueObjects;

namespace ScoringAudit.Domain.Events;

/// <summary>
/// Disparado por TeamLedger cada vez que su TotalScore cambia.
///
/// Consumidores:
/// — SignalR Hub (LiveEngine.API): empuja el nuevo ranking en tiempo real
///   a las pantallas del Operador y los Equipos (RF-12, Flujo 3 del DDD).
///
/// Incluye el ranking completo pre-calculado para que SignalR
/// solo tenga que hacer broadcast — sin consultas adicionales.
/// </summary>
public sealed record TeamScoreUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public int NewTotalScore { get; init; }

    /// <summary>
    /// Ranking completo y ordenado de la sesión en el momento del evento.
    /// Listo para broadcast via SignalR sin queries adicionales.
    /// </summary>
    public IReadOnlyList<RankingEntry> UpdatedRanking { get; init; } = [];
}