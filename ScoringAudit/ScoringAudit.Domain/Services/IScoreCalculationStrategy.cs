using ScoringAudit.Domain.ValueObjects;

namespace ScoringAudit.Domain.Services;

/// <summary>
/// Interfaz del patrón Strategy para el cálculo de puntaje.
///
/// Cada implementación encapsula la lógica de puntuación
/// para un tipo de nodo específico (Trivia, TreasureHunt, etc.).
/// ScoreCalculatorService selecciona la estrategia correcta
/// según el NodeType que viaja en EvidenceValidatedEvent.
///
/// OCP (Open/Closed Principle): para agregar un nuevo tipo de nodo
/// con su propia lógica de puntaje, solo se agrega una nueva
/// implementación de esta interfaz — nada más cambia.
/// </summary>
public interface IScoreCalculationStrategy
{
    /// <summary>
    /// Tipo de nodo que esta estrategia sabe calcular.
    /// Debe coincidir exactamente con el valor de MissionNodeType
    /// serializado en EvidenceValidatedEvent.NodeType.
    /// </summary>
    string NodeType { get; }

    /// <summary>
    /// Calcula el puntaje final para una evidencia validada.
    /// </summary>
    /// <param name="baseScore">Puntaje base definido en el nodo de la misión.</param>
    /// <param name="difficultyMultiplier">Multiplicador de dificultad de la misión.</param>
    /// <param name="elapsedSeconds">
    /// Segundos transcurridos desde el inicio de la sesión.
    /// Algunas estrategias pueden aplicar bonificación por velocidad.
    /// </param>
    /// <returns>Puntaje final entero a registrar en el ScoreEntry.</returns>
    int Calculate(int baseScore, decimal difficultyMultiplier, double elapsedSeconds);
}