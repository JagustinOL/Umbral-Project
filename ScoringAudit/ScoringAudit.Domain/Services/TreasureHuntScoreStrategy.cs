namespace ScoringAudit.Domain.Services;

/// <summary>
/// Estrategia de cálculo para nodos de tipo TreasureHunt.
///
/// Lógica:
///   puntaje = baseScore × difficultyMultiplier
///
/// La búsqueda del tesoro no aplica bonificación de velocidad —
/// el desafío está en encontrar el objeto, no en la rapidez.
/// El tiempo sí se usa como criterio de DESEMPATE en el ranking (RB-08),
/// pero no afecta directamente el puntaje de este tipo de nodo.
/// </summary>
public sealed class TreasureHuntScoreStrategy : IScoreCalculationStrategy
{
    public string NodeType => "TreasureHunt";

    public int Calculate(int baseScore, decimal difficultyMultiplier, double elapsedSeconds)
    {
        if (baseScore <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseScore),
                "El puntaje base debe ser mayor que cero.");
        if (difficultyMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(difficultyMultiplier),
                "El multiplicador de dificultad debe ser mayor que cero.");

        var raw = baseScore * difficultyMultiplier;
        return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
    }
}