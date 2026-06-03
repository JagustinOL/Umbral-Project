using SessionManagement.Domain.Common;

namespace SessionManagement.Domain.Entities;

/// <summary>
/// Registro inmutable de una pista liberada a un equipo específico
/// en el contexto de una LiveSession.
///
/// RB-04: Una pista no puede liberarse dos veces al mismo equipo
/// para la misma etapa. LiveSession verifica la existencia de un
/// ReleasedHint con el mismo (TeamId + HintId) antes de liberar.
///
/// Esta entidad es el "comprobante" de la liberación.
/// </summary>
public sealed class ReleasedHint : Entity
{
    public Guid TeamId { get; private set; }

    /// <summary>
    /// ID de la Hint en el MissionManagement context.
    /// Solo referencia por ID — no cargamos el objeto completo.
    /// </summary>
    public Guid HintId { get; private set; }

    /// <summary>ID del nodo al que pertenece la pista (para agrupar en el panel del equipo).</summary>
    public Guid MissionNodeId { get; private set; }

    /// <summary>Puntos de penalización que se restarán al equipo por usar esta pista.</summary>
    public int PenaltyPoints { get; private set; }

    public DateTime ReleasedAtUtc { get; private set; }

    /// <summary>
    /// Indica si la liberación fue manual (por el Operador)
    /// o automática (por regla de avance del sistema).
    /// </summary>
    public bool WasManualRelease { get; private set; }

    private ReleasedHint() { }

    public static ReleasedHint Create(
        Guid teamId,
        Guid hintId,
        Guid missionNodeId,
        int penaltyPoints,
        bool wasManualRelease)
    {
        if (teamId == Guid.Empty)
            throw new ArgumentException("TeamId no puede ser vacío.", nameof(teamId));
        if (hintId == Guid.Empty)
            throw new ArgumentException("HintId no puede ser vacío.", nameof(hintId));
        if (missionNodeId == Guid.Empty)
            throw new ArgumentException("MissionNodeId no puede ser vacío.", nameof(missionNodeId));
        if (penaltyPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(penaltyPoints),
                "La penalización no puede ser negativa.");

        return new ReleasedHint
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            HintId = hintId,
            MissionNodeId = missionNodeId,
            PenaltyPoints = penaltyPoints,
            WasManualRelease = wasManualRelease,
            ReleasedAtUtc = DateTime.UtcNow
        };
    }
}