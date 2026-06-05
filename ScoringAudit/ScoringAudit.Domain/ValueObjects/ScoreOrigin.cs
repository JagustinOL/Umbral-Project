namespace ScoringAudit.Domain.ValueObjects;

/// <summary>
/// Value Object que describe el origen de un ScoreEntry positivo.
/// Complementa a PenaltyReason para los puntos ganados.
///
/// RB-07: El puntaje acumulado no puede quedar sin trazabilidad de origen.
/// Todo ScoreEntry — positivo o negativo — debe tener un origen documentado.
/// ScoreOrigin cubre los casos positivos; PenaltyReason cubre los negativos.
/// </summary>
public sealed record ScoreOrigin(
    Guid MissionNodeId,
    string NodeType,
    int BaseScore,
    decimal DifficultyMultiplier,
    double ElapsedSeconds
)
{
    /// <summary>
    /// Puntaje final calculado que este entry aporta al TeamLedger.
    /// Se almacena para que la trazabilidad sea directamente legible
    /// sin tener que recalcular.
    /// </summary>
    public int ComputedScore => (int)Math.Round(BaseScore * DifficultyMultiplier);

    public override string ToString() =>
        $"Nodo {NodeType} ({MissionNodeId}) | " +
        $"Base: {BaseScore} × {DifficultyMultiplier} = {ComputedScore} pts | " +
        $"Tiempo: {ElapsedSeconds:F1}s";
}