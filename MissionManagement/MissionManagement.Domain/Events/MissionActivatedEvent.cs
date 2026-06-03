using MissionManagement.Domain.Common;

namespace MissionManagement.Domain.Events;

/// <summary>
/// Evento de dominio disparado cuando una Misión pasa de Borrador → Activa.
///
/// LiveEngine.API escucha este evento para saber que la misión ya está
/// disponible para la creación de sesiones (cumple RB-01).
///
/// Contiene AllowedNodeIds: la lista de IDs de nodos hoja válidos para
/// recibir evidencias. LiveSession los cargará al crearse, resolviendo RB-05
/// sin llamadas síncronas entre microservicios.
/// </summary>
public sealed record MissionActivatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid MissionId { get; init; }
    public string MissionTitle { get; init; } = string.Empty;

    /// <summary>
    /// IDs de nodos hoja activados con su puntaje base y tipo.
    /// LiveEngine los almacena localmente como snapshot para validar RB-05.
    /// </summary>
    public IReadOnlyList<ActivatedNodeSnapshot> AllowedNodes { get; init; } = [];

    /// <summary>
    /// Multiplicador de dificultad para que ScoringAudit aplique
    /// la estrategia correcta (Easy/Medium/Hard).
    /// </summary>
    public decimal DifficultyMultiplier { get; init; }
}

/// <summary>
/// Snapshot ligero de un nodo para viajar en el evento.
/// No es una entidad completa — solo los datos que LiveEngine necesita.
/// </summary>
public sealed record ActivatedNodeSnapshot(
    Guid NodeId,
    string NodeType,
    int BaseScore
);