using ScoringAudit.Domain.Exceptions;
using ScoringAudit.Domain.ValueObjects;

namespace ScoringAudit.Domain.Services;

/// <summary>
/// Domain Service que orquesta el patrón Strategy para calcular puntajes.
///
/// ¿Por qué no está en TeamLedger?
/// Porque el cálculo depende de múltiples factores externos al historial
/// del equipo (NodeType, DifficultyMultiplier, ElapsedSeconds) y la lógica
/// es intercambiable. TeamLedger solo sabe sumar su propio historial.
///
/// DIP: recibe las estrategias por inyección de dependencias —
/// no las instancia directamente. El contenedor de IoC registra
/// todas las IScoreCalculationStrategy disponibles.
///
/// OCP: agregar TreasureHuntV2Strategy no requiere modificar esta clase.
/// </summary>
public sealed class ScoreCalculatorService
{
    private readonly IReadOnlyDictionary<string, IScoreCalculationStrategy> _strategies;

    /// <summary>
    /// El contenedor inyecta IEnumerable con todas las estrategias registradas.
    /// Se indexan por NodeType para O(1) lookup.
    /// </summary>
    public ScoreCalculatorService(IEnumerable<IScoreCalculationStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);

        _strategies = strategies.ToDictionary(
            s => s.NodeType,
            s => s,
            StringComparer.OrdinalIgnoreCase);

        if (_strategies.Count == 0)
            throw new ScoringDomainException(
                "ScoreCalculatorService requiere al menos una estrategia de cálculo. " +
                "Registra TriviaScoreStrategy y TreasureHuntScoreStrategy en el contenedor IoC.");
    }

    /// <summary>
    /// Calcula el puntaje y construye el ScoreOrigin listo para crear
    /// el ScoreEntry en TeamLedger.
    ///
    /// Escucha EvidenceValidatedEvent y produce un ScoreOrigin que
    /// TeamLedger.AddEvidenceScore() usa para generar el ScoreEntry inmutable.
    /// </summary>
    public ScoreOrigin Calculate(
        Guid missionNodeId,
        string nodeType,
        int baseScore,
        decimal difficultyMultiplier,
        double elapsedSeconds)
    {
        if (!_strategies.TryGetValue(nodeType, out var strategy))
            throw new ScoringDomainException(
                $"No existe una estrategia de cálculo para el tipo de nodo '{nodeType}'. " +
                $"Tipos disponibles: {string.Join(", ", _strategies.Keys)}. " +
                $"Registra una implementación de IScoreCalculationStrategy para este tipo.");

        int computed = strategy.Calculate(baseScore, difficultyMultiplier, elapsedSeconds);

        return new ScoreOrigin(
            MissionNodeId: missionNodeId,
            NodeType: nodeType,
            BaseScore: baseScore,
            DifficultyMultiplier: difficultyMultiplier,
            ElapsedSeconds: elapsedSeconds
        );
    }
}