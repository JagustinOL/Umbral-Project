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
    double ElapsedSeconds,
    int? FinalScore = null
)
{
    /// <summary>
    /// Puntaje final calculado por la Strategy y persistido en el ledger.
    /// Si no se provee FinalScore, se usa el cálculo base (sin bonificación).
    /// </summary>
    public int ComputedScore => FinalScore ?? (int)Math.Round(BaseScore * DifficultyMultiplier);

    public static ScoreOrigin FromStrategyResult(
        Guid missionNodeId,
        string nodeType,
        int baseScore,
        decimal difficultyMultiplier,
        double elapsedSeconds,
        int strategyScore) =>
        new(missionNodeId, nodeType, baseScore, difficultyMultiplier, elapsedSeconds, strategyScore);

    public override string ToString() =>
        $"Nodo {NodeType} ({MissionNodeId}) | " +
        $"Base: {BaseScore} × {DifficultyMultiplier} = {ComputedScore} pts | " +
        $"Tiempo: {ElapsedSeconds:F1}s";
}