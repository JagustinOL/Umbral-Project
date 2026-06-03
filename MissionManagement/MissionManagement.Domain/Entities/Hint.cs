using MissionManagement.Domain.Common;

namespace MissionManagement.Domain.Entities;

/// <summary>
/// Representa una pista asociada a un MissionNode específico.
///
/// RB-04: Una pista no puede liberarse dos veces al mismo equipo para la misma etapa.
/// Esta regla se enforza en LiveEngine al momento de liberar la pista, pero el diseño
/// de esta entidad la hace identificable de forma única por (MissionNodeId + Order).
/// </summary>
public sealed class Hint : Entity
{
    /// <summary>
    /// Orden de la pista dentro del nodo (1 = primera pista, 2 = segunda, etc.).
    /// Determina el orden en que deben liberarse.
    /// </summary>
    public int Order { get; private set; }

    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Penalización de puntaje al liberar esta pista.
    /// Valor positivo que se RESTA del puntaje del equipo (ej: 10 = -10 puntos).
    /// </summary>
    public int PenaltyPoints { get; private set; }

    /// <summary>
    /// FK lógica al MissionNode dueño. Solo se referencia por ID
    /// para mantener las fronteras del agregado.
    /// </summary>
    public Guid MissionNodeId { get; private set; }

    private Hint() { }

    public static Hint Create(
        Guid missionNodeId,
        int order,
        string content,
        int penaltyPoints = 0)
    {
        if (missionNodeId == Guid.Empty)
            throw new ArgumentException("El MissionNodeId no puede ser vacío.", nameof(missionNodeId));
        if (order < 1)
            throw new ArgumentOutOfRangeException(nameof(order), "El orden debe ser mayor o igual a 1.");
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("El contenido de la pista no puede estar vacío.", nameof(content));
        if (penaltyPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(penaltyPoints),
                "La penalización no puede ser negativa. Use 0 para pistas sin costo.");

        return new Hint
        {
            Id = Guid.NewGuid(),
            MissionNodeId = missionNodeId,
            Order = order,
            Content = content,
            PenaltyPoints = penaltyPoints
        };
    }

    /// <summary>Actualiza el contenido de la pista. Solo válido mientras la Misión es Borrador.</summary>
    internal void UpdateContent(string newContent)
    {
        if (string.IsNullOrWhiteSpace(newContent))
            throw new ArgumentException("El contenido no puede estar vacío.", nameof(newContent));
        Content = newContent;
    }
}