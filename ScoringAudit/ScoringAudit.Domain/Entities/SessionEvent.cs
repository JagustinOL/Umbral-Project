using Common;

namespace ScoringAudit.Domain.Entities;

/// <summary>
/// Entidad inmutable que representa un evento significativo
/// registrado en el AuditLog de una sesión.
///
/// RF-15: Debe existir un historial de eventos de la sesión
/// con trazabilidad mínima para auditoría.
///
/// Al igual que ScoreEntry, una vez persistido nunca se modifica.
/// El AuditLog es append-only por diseño.
/// </summary>
public sealed class SessionEvent : Entity
{
    public Guid SessionId { get; private set; }

    /// <summary>
    /// Tipo del evento ocurrido. Permite filtrar y categorizar
    /// el historial de auditoría sin parsear texto libre.
    /// </summary>
    public SessionEventType EventType { get; private set; }

    /// <summary>
    /// ID del evento de dominio original que originó este registro.
    /// Correlaciona el AuditLog con el bus de mensajes (RabbitMQ).
    /// </summary>
    public Guid SourceEventId { get; private set; }

    /// <summary>Equipo involucrado en el evento. Null para eventos de sesión global.</summary>
    public Guid? TeamRef { get; private set; }

    /// <summary>Nodo involucrado. Null para eventos que no son de nodo.</summary>
    public Guid? MissionNodeRef { get; private set; }

    /// <summary>
    /// Descripción legible del evento para el panel de auditoría.
    /// Generada automáticamente al crear el evento.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Metadatos adicionales en JSON. Null si no aplica.</summary>
    public string? Metadata { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    private SessionEvent() { }

    /// <summary>
    /// Fábrica interna. Solo AuditLog puede crear SessionEvents.
    /// </summary>
    internal static SessionEvent Create(
        Guid sessionId,
        SessionEventType eventType,
        Guid sourceEventId,
        string description,
        Guid? teamRef = null,
        Guid? missionNodeRef = null,
        string? metadata = null)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId no puede ser vacío.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción del evento no puede estar vacía.", nameof(description));

        return new SessionEvent
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            EventType = eventType,
            SourceEventId = sourceEventId,
            Description = description,
            TeamRef = teamRef,
            MissionNodeRef = missionNodeRef,
            Metadata = metadata,
            OccurredAtUtc = DateTime.UtcNow
        };
    }
}