using Common;
using ScoringAudit.Domain.Exceptions;
using ScoringAudit.Domain.ValueObjects;

namespace ScoringAudit.Domain.Entities;

/// <summary>
/// Entidad inmutable que representa una entrada en el libro mayor del equipo.
/// Puede ser positiva (evidencia validada) o negativa (penalización o pista).
///
/// RB-07: La propiedad TotalScore del TeamLedger no puede modificarse
/// directamente. Se recalcula SIEMPRE sumando el historial de ScoreEntries.
/// Esta entidad es el registro inmutable que garantiza esa trazabilidad.
///
/// Una vez creado, un ScoreEntry NUNCA se modifica ni elimina.
/// Si un operador comete un error, se crea un nuevo ScoreEntry
/// compensatorio (patrón de registro contable / event sourcing lite).
/// </summary>
public sealed class ScoreEntry : Entity
{
    /// <summary>
    /// Monto de puntos. Positivo = ganado, Negativo = penalización.
    /// Nunca cero — un entry de cero no tiene sentido contable.
    /// </summary>
    public int Points { get; private set; }

    public DateTime RecordedAtUtc { get; private set; }

    public ScoreEntryType EntryType { get; private set; }

    // ── Datos de trazabilidad (uno de los dos será no-null) ────────────────────

    /// <summary>No-null cuando EntryType = EvidenceRewarded.</summary>
    public ScoreOrigin? Origin { get; private set; }

    /// <summary>No-null cuando EntryType = ManualPenalty | HintPenalty | TimePenalty.</summary>
    public PenaltyReason? PenaltyReason { get; private set; }

    /// <summary>
    /// ID del evento de dominio que originó este entry.
    /// Permite correlacionar el ledger con el AuditLog para debugging.
    /// </summary>
    public Guid SourceEventId { get; private set; }

    private ScoreEntry() { }

    /// <summary>
    /// Crea un ScoreEntry positivo por evidencia validada.
    /// Llamado exclusivamente desde TeamLedger.AddEvidenceScore().
    /// </summary>
    internal static ScoreEntry ForEvidence(ScoreOrigin origin, Guid sourceEventId)
    {
        ArgumentNullException.ThrowIfNull(origin);

        if (origin.ComputedScore <= 0)
            throw new ScoringDomainException(
                $"El puntaje calculado para el nodo {origin.MissionNodeId} " +
                $"debe ser mayor que cero. Valor calculado: {origin.ComputedScore}.");

        return new ScoreEntry
        {
            Id = Guid.NewGuid(),
            Points = origin.ComputedScore,
            EntryType = ScoreEntryType.EvidenceRewarded,
            Origin = origin,
            PenaltyReason = null,
            SourceEventId = sourceEventId,
            RecordedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Crea un ScoreEntry negativo por penalización.
    /// Llamado exclusivamente desde TeamLedger.ApplyPenalty().
    ///
    /// RB-06: penaltyReason no puede ser null.
    /// </summary>
    internal static ScoreEntry ForPenalty(
        int penaltyPoints,
        PenaltyReason reason,
        ScoreEntryType entryType,
        Guid sourceEventId)
    {
        ArgumentNullException.ThrowIfNull(reason);

        if (penaltyPoints <= 0)
            throw new ScoringDomainException(
                "Los puntos de penalización deben ser un valor positivo. " +
                "Se almacenarán como negativo internamente.");

        if (entryType is ScoreEntryType.EvidenceRewarded)
            throw new ScoringDomainException(
                $"El tipo '{entryType}' no es válido para un ScoreEntry de penalización.");

        return new ScoreEntry
        {
            Id = Guid.NewGuid(),
            Points = -penaltyPoints,   // se guarda negativo
            EntryType = entryType,
            PenaltyReason = reason,
            Origin = null,
            SourceEventId = sourceEventId,
            RecordedAtUtc = DateTime.UtcNow
        };
    }
}