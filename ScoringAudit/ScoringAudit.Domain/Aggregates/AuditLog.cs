using Common;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Exceptions;

namespace ScoringAudit.Domain.Aggregates;

/// <summary>
/// AGGREGATE ROOT — AuditLog.
///
/// Registro histórico e inmutable de todos los eventos significativos
/// de una sesión. Es append-only: los eventos se agregan pero nunca
/// se modifican ni eliminan.
///
/// RF-15: Debe existir un historial de eventos de la sesión
/// con trazabilidad mínima para auditoría.
///
/// Ciclo de vida:
///   Open  → activo mientras la sesión está en curso.
///   Closed → sellado al recibir SessionFinalizedEvent o Cancelled.
///            Una vez cerrado, no acepta nuevos eventos.
/// </summary>
public sealed class AuditLog : AggregateRoot
{
    private readonly List<SessionEvent> _events = [];

    public Guid SessionRef { get; private set; }
    public bool IsClosed { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    public IReadOnlyList<SessionEvent> Events => _events.AsReadOnly();

    private AuditLog() { }

    // ── Fábrica ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo AuditLog al recibir SessionStartedEvent.
    /// Un AuditLog = una sesión.
    /// </summary>
    public static AuditLog Create(Guid sessionRef)
    {
        if (sessionRef == Guid.Empty)
            throw new ArgumentException("SessionRef no puede ser vacío.", nameof(sessionRef));

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            SessionRef = sessionRef,
            IsClosed = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    // ── Comportamiento ─────────────────────────────────────────────────────────

    /// <summary>
    /// Registra un evento genérico en el historial.
    /// INVARIANTE: no acepta eventos si el log está cerrado.
    /// </summary>
    public void RecordEvent(
        SessionEventType eventType,
        Guid sourceEventId,
        string description,
        Guid? teamRef = null,
        Guid? missionNodeRef = null,
        string? metadata = null)
    {
        ThrowIfClosed();

        var sessionEvent = SessionEvent.Create(
            sessionId: SessionRef,
            eventType: eventType,
            sourceEventId: sourceEventId,
            description: description,
            teamRef: teamRef,
            missionNodeRef: missionNodeRef,
            metadata: metadata);

        _events.Add(sessionEvent);
    }

    /// <summary>
    /// Cierra el AuditLog. Llamado al recibir SessionFinalizedEvent o Cancelled.
    /// Una vez cerrado es inmutable — ningún evento adicional puede registrarse.
    /// </summary>
    public void Close()
    {
        if (IsClosed)
            throw new ScoringDomainException(
                $"El AuditLog de la sesión {SessionRef} ya está cerrado.");

        IsClosed = true;
        ClosedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Retorna todos los eventos de un equipo específico.
    /// Útil para el panel de progreso del equipo (RF-06).
    /// </summary>
    public IReadOnlyList<SessionEvent> GetEventsByTeam(Guid teamRef) =>
        _events
            .Where(e => e.TeamRef == teamRef)
            .OrderBy(e => e.OccurredAtUtc)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Retorna todos los eventos de un tipo específico para la sesión.
    /// Útil para el panel de auditoría del Operador (RF-13, RF-15).
    /// </summary>
    public IReadOnlyList<SessionEvent> GetEventsByType(SessionEventType type) =>
        _events
            .Where(e => e.EventType == type)
            .OrderBy(e => e.OccurredAtUtc)
            .ToList()
            .AsReadOnly();

    // ── Helper privado ─────────────────────────────────────────────────────────

    private void ThrowIfClosed()
    {
        if (IsClosed)
            throw new ScoringDomainException(
                $"No se pueden registrar eventos en el AuditLog de la sesión {SessionRef} " +
                $"porque ya está cerrado (sesión finalizada o cancelada).");
    }
}