namespace ScoringAudit.Domain.Services;

/// <summary>
/// Estrategia de cálculo para nodos de tipo Trivia.
///
/// Lógica:
///   puntaje = baseScore × difficultyMultiplier × bonificaciónVelocidad
///
/// BonificaciónVelocidad:
///   — Menos de 30s  → ×1.20 (respuesta muy rápida)
///   — Menos de 60s  → ×1.10
///   — Menos de 120s → ×1.00 (sin bonificación)
///   — Más de 120s   → ×0.90 (penalización por lentitud)
///
/// La bonificación incentiva el conocimiento ágil en trivias.
/// </summary>
public sealed class TriviaScoreStrategy : IScoreCalculationStrategy
{
    public string NodeType => "Trivia";

    public int Calculate(int baseScore, decimal difficultyMultiplier, double elapsedSeconds)
    {
        if (baseScore <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseScore),
                "El puntaje base debe ser mayor que cero.");
        if (difficultyMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(difficultyMultiplier),
                "El multiplicador de dificultad debe ser mayor que cero.");

        decimal speedBonus = elapsedSeconds switch
        {
            < 30  => 1.20m,
            < 60  => 1.10m,
            < 120 => 1.00m,
            _     => 0.90m
        };

        var raw = baseScore * difficultyMultiplier * speedBonus;
        return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
    }
}