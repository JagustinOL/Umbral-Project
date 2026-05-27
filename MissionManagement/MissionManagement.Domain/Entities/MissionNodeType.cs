namespace MissionManagement.Domain.Entities;

/// <summary>
/// Enumera los tipos de nodos disponibles en una Misión.
/// El tipo determina la estrategia de cálculo de puntaje
/// que aplicará ScoreCalculatorService en ScoringAudit.
/// </summary>
public enum MissionNodeType
{
    /// <summary>Etapa contenedora de sub-nodos. No se completa directamente con evidencias.</summary>
    Stage = 0,

    /// <summary>Pregunta de conocimiento. Respuesta textual validada contra una clave.</summary>
    Trivia = 1,

    /// <summary>Búsqueda de objeto físico o código QR. Evidencia fotográfica.</summary>
    TreasureHunt = 2
}