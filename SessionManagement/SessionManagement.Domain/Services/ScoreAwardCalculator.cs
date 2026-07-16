namespace SessionManagement.Domain.Services;

/// <summary>
/// Calcula los puntos acreditados al completar un nodo jugable.
/// Debe coincidir con ScoringAudit (TriviaScoreStrategy / TreasureHuntScoreStrategy):
///   awarded = round(baseScore × difficultyMultiplier)
/// </summary>
public static class ScoreAwardCalculator
{
    public static int Compute(int baseScore, decimal difficultyMultiplier)
    {
        if (baseScore <= 0)
            return 0;
        if (difficultyMultiplier <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(difficultyMultiplier),
                "El multiplicador de dificultad debe ser mayor que cero.");

        var raw = baseScore * difficultyMultiplier;
        return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
    }
}
