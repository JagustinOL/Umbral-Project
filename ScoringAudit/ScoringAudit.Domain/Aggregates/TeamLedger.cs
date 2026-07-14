using Common;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Events;
using ScoringAudit.Domain.Exceptions;
using ScoringAudit.Domain.ValueObjects;

namespace ScoringAudit.Domain.Aggregates;

/// <summary>
/// AGGREGATE ROOT — TeamLedger (Libro Mayor del Equipo).
///
/// Mantiene el historial contable inmutable de puntos de un equipo
/// dentro de una sesión específica. Es el único lugar donde los puntos
/// se calculan y acumulan.
///
/// INVARIANTES:
/// — RB-07: TotalScore NUNCA se asigna directamente.
///          Se recalcula siempre sumando el historial de ScoreEntries.
/// — RB-06: ApplyPenalty() exige un PenaltyReason con descripción válida.
/// — Un nodo no puede recompensarse dos veces en el mismo ledger
///   (un equipo no gana puntos por el mismo nodo dos veces).
/// — Un ledger cerrado (sesión finalizada) no acepta nuevas entradas.
/// </summary>
public sealed class TeamLedger : AggregateRoot
{
    private readonly List<ScoreEntry> _entries = [];

    public Guid TeamRef { get; private set; }
    public Guid SessionRef { get; private set; }

    /// <summary>Nombre del equipo. Copiado para que el ranking no necesite joins.</summary>
    public string TeamName { get; private set; } = string.Empty;

    public bool IsClosed { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public double? CompletionElapsedSeconds { get; private set; }

    public IReadOnlyList<ScoreEntry> Entries => _entries.AsReadOnly();

    // ── Propiedades calculadas (RB-07) ─────────────────────────────────────────

    /// <summary>
    /// Puntaje total calculado sumando el historial inmutable.
    /// RB-07: Nunca se asigna directamente — siempre derivado de _entries.
    /// </summary>
    public int TotalScore => _entries.Sum(e => e.Points);

    /// <summary>
    /// Cantidad de nodos completados correctamente (entradas positivas únicas).
    /// Expuesto para RankingManagerService.
    /// </summary>
    public int CompletedNodesCount =>
        _entries
            .Where(e => e.EntryType == ScoreEntryType.EvidenceRewarded)
            .Select(e => e.Origin!.MissionNodeId)
            .Distinct()
            .Count();

    /// <summary>Cantidad de penalizaciones aplicadas.</summary>
    public int PenaltiesCount =>
        _entries.Count(e => e.Points < 0);

    /// <summary>
    /// ElapsedSeconds del último ScoreEntry positivo.
    /// Criterio de desempate en RankingManagerService (RB-08):
    /// ante igual puntaje, gana el equipo con menor tiempo.
    /// </summary>
    public double LastPositiveEntryElapsedSeconds =>
        _entries
            .Where(e => e.EntryType == ScoreEntryType.EvidenceRewarded
                     && e.Origin is not null)
            .OrderByDescending(e => e.RecordedAtUtc)
            .FirstOrDefault()
            ?.Origin?.ElapsedSeconds ?? 0;

    /// <summary>Tiempo definitivo reportado al completar la misión.</summary>
    public double TotalElapsedSeconds => CompletionElapsedSeconds ?? LastPositiveEntryElapsedSeconds;

    private TeamLedger() { }

    // ── Fábrica ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo TeamLedger al recibir TeamRegisteredEvent.
    /// Puntaje inicial = 0 (sin entradas).
    /// </summary>
    public static TeamLedger Create(Guid teamRef, Guid sessionRef, string teamName)
    {
        if (teamRef == Guid.Empty)
            throw new ArgumentException("TeamRef no puede ser vacío.", nameof(teamRef));
        if (sessionRef == Guid.Empty)
            throw new ArgumentException("SessionRef no puede ser vacío.", nameof(sessionRef));
        if (string.IsNullOrWhiteSpace(teamName))
            throw new ArgumentException("TeamName no puede estar vacío.", nameof(teamName));

        return new TeamLedger
        {
            Id = Guid.NewGuid(),
            TeamRef = teamRef,
            SessionRef = sessionRef,
            TeamName = teamName.Trim(),
            IsClosed = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    // ── Comportamiento ─────────────────────────────────────────────────────────

    /// <summary>
    /// Registra un ScoreEntry positivo por evidencia validada.
    ///
    /// Llamado por el Application Service tras calcular el puntaje
    /// con ScoreCalculatorService (patrón Strategy).
    ///
    /// INVARIANTE: Un nodo no puede recompensarse dos veces.
    ///
    /// Dispara: TeamScoreUpdatedEvent → SignalR broadcast (Flujo 3).
    /// </summary>
    public void AddEvidenceScore(ScoreOrigin origin, Guid sourceEventId)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ThrowIfClosed();

        // Un equipo no puede ganar puntos dos veces por el mismo nodo
        bool alreadyRewarded = _entries
            .Any(e => e.EntryType == ScoreEntryType.EvidenceRewarded
                   && e.Origin?.MissionNodeId == origin.MissionNodeId);

        if (alreadyRewarded)
            throw new ScoringDomainException(
                $"El equipo {TeamRef} ya recibió puntos por el nodo {origin.MissionNodeId} " +
                $"en la sesión {SessionRef}. No puede recompensarse dos veces por el mismo nodo.");

        var entry = ScoreEntry.ForEvidence(origin, sourceEventId);
        _entries.Add(entry);

        RaiseDomainEvent(new TeamScoreUpdatedEvent
        {
            SessionId = SessionRef,
            TeamId = TeamRef,
            NewTotalScore = TotalScore
        });
    }

    /// <summary>
    /// Registra un ScoreEntry negativo por penalización.
    ///
    /// RB-06: PenaltyReason es obligatorio y debe ser construido con
    /// PenaltyReason.ForManualPenalty() o métodos equivalentes.
    ///
    /// Dispara: TeamScoreUpdatedEvent → SignalR broadcast.
    /// </summary>
    public void ApplyPenalty(
        int penaltyPoints,
        PenaltyReason reason,
        ScoreEntryType entryType,
        Guid sourceEventId)
    {
        // RB-06: PenaltyReason no puede ser null (ya garantizado por su propio constructor)
        ArgumentNullException.ThrowIfNull(reason);
        ThrowIfClosed();

        if (penaltyPoints <= 0)
            throw new ScoringDomainException(
                "Los puntos de penalización deben ser positivos. " +
                "Se registrarán como negativos en el historial.");

        var entry = ScoreEntry.ForPenalty(penaltyPoints, reason, entryType, sourceEventId);
        _entries.Add(entry);

        RaiseDomainEvent(new TeamScoreUpdatedEvent
        {
            SessionId = SessionRef,
            TeamId = TeamRef,
            NewTotalScore = TotalScore
        });
    }

    /// <summary>
    /// Cierra el ledger cuando la sesión finaliza o se cancela.
    /// Reacción a SessionFinalizedEvent.
    /// Una vez cerrado, no se aceptan nuevas entradas.
    /// </summary>
    public void Close()
    {
        if (IsClosed)
            throw new ScoringDomainException(
                $"El TeamLedger del equipo {TeamRef} en la sesión {SessionRef} ya está cerrado.");

        IsClosed = true;

        RaiseDomainEvent(new TeamScoreUpdatedEvent
        {
            SessionId = SessionRef,
            TeamId = TeamRef,
            NewTotalScore = TotalScore
        });
    }

    public void SetCompletionElapsedSeconds(double elapsedSeconds)
    {
        ThrowIfClosed();
        if (elapsedSeconds < 0)
            throw new ScoringDomainException("El tiempo transcurrido no puede ser negativo.");

        CompletionElapsedSeconds = elapsedSeconds;
    }

    // ── Helper privado ─────────────────────────────────────────────────────────

    private void ThrowIfClosed()
    {
        if (IsClosed)
            throw new ScoringDomainException(
                $"El TeamLedger del equipo {TeamRef} está cerrado. " +
                $"No se pueden registrar nuevas entradas en una sesión finalizada.");
    }
}