using SessionManagement.Domain.Common;

namespace SessionManagement.Domain.Events;

/// <summary>
/// El evento más importante del sistema (Flujo 2 del DDD).
///
/// Disparado cuando LiveSession acepta y valida una evidencia correcta.
/// LiveEngine NO calcula puntos — solo avisa que la evidencia es válida.
///
/// Consumidores:
/// — ScoringAudit: aplica el patrón Strategy según NodeType y
///   DifficultyMultiplier para crear un ScoreEntry positivo en TeamLedger.
/// — SignalR Hub: puede notificar al equipo que su respuesta fue aceptada.
///
/// Incluye DifficultyMultiplier y NodeType para que ScoringAudit
/// calcule el puntaje sin consultar a MissionManagement (desacoplamiento).
/// </summary>
public sealed record EvidenceValidatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public Guid EvidenceSubmissionId { get; init; }
    public Guid TeamId { get; init; }
    public Guid MissionNodeId { get; init; }

    /// <summary>
    /// Tipo del nodo (Trivia, TreasureHunt) para seleccionar
    /// la estrategia de cálculo en ScoreCalculatorService.
    /// </summary>
    public string NodeType { get; init; } = string.Empty;

    /// <summary>Título del nodo (snapshot) para auditoría legible.</summary>
    public string NodeTitle { get; init; } = string.Empty;

    /// <summary>Puntaje base del nodo copiado desde AllowedNode snapshot.</summary>
    public int BaseScore { get; init; }

    /// <summary>
    /// Multiplicador de dificultad de la misión.
    /// Copiado desde el snapshot de la sesión para evitar llamadas cruzadas.
    /// </summary>
    public decimal DifficultyMultiplier { get; init; }

    /// <summary>
    /// Tiempo en segundos que tardó el equipo en responder desde
    /// que se inició la sesión. Usado por RankingManagerService
    /// como criterio de desempate (RB-08).
    /// </summary>
    public double ElapsedSeconds { get; init; }
}