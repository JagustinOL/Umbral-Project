namespace ScoringAudit.Domain.Services;

/// <summary>
/// Estrategia de cálculo para nodos de tipo Trivia.
///
/// Lógica (RN-08 / RN-09):
///   puntaje = baseScore × difficultyMultiplier
///
/// El tiempo NO modifica el puntaje: RN-09 lo usa solo como
/// criterio de desempate en el ranking (RankingManagerService).
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

        // elapsedSeconds se recibe por contrato Strategy, pero RN-09 lo reserva al ranking.
        _ = elapsedSeconds;

        var raw = baseScore * difficultyMultiplier;
        return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
    }
}
