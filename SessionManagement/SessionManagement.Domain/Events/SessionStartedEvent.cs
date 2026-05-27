using Common;

namespace SessionManagement.Domain.Events;

/// <summary>
/// Disparado cuando una LiveSession transiciona a estado Active.
///
/// Consumidores:
/// — ScoringAudit: crea el registro de inicio en el AuditLog.
/// — Team aggregate: bloquea a los equipos (IsLocked = true) para
///   que los jugadores no puedan abandonar durante la sesión activa.
/// </summary>
public sealed record SessionStartedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public Guid MissionRef { get; init; }
    public Guid OperatorRef { get; init; }
    public IReadOnlyList<Guid> ParticipatingTeamIds { get; init; } = [];
    public DateTime StartedAtUtc { get; init; }
}