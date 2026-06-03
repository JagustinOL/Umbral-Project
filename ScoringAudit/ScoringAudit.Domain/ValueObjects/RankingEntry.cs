namespace ScoringAudit.Domain.ValueObjects;

/// <summary>
/// Value Object de solo lectura que representa la posición de un equipo
/// en el ranking de una sesión.
///
/// RB-08: El ranking debe ordenarse de mayor a menor puntaje,
/// usando el tiempo de resolución como criterio de desempate.
///
/// Producido por RankingManagerService y enviado a través de
/// TeamScoreUpdatedEvent hacia el Hub de SignalR para actualización
/// en tiempo real (RF-12).
/// </summary>
public sealed record RankingEntry(
    int Position,
    Guid TeamId,
    string TeamName,
    int TotalScore,
    /// <summary>
    /// Segundos transcurridos desde el inicio hasta el último ScoreEntry positivo.
    /// Criterio de desempate cuando dos equipos tienen el mismo TotalScore (RB-08).
    /// </summary>
    double TotalElapsedSeconds,
    int CompletedNodes,
    int PenaltiesApplied
)
{
    public override string ToString() =>
        $"#{Position} {TeamName} — {TotalScore} pts ({TotalElapsedSeconds:F1}s)";
}