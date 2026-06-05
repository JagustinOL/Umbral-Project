namespace ScoringAudit.Domain.Entities;

/// <summary>
/// Clasifica el origen de un ScoreEntry para trazabilidad y reportes.
/// Complementa a PenaltyReason/ScoreOrigin en la auditoría del ledger.
/// </summary>
public enum ScoreEntryType
{
    /// <summary>Puntos ganados por evidencia correctamente validada.</summary>
    EvidenceRewarded = 0,

    /// <summary>Descuento por penalización manual del Operador (RB-06).</summary>
    ManualPenalty = 1,

    /// <summary>Descuento automático por uso de pista (Flujo 5).</summary>
    HintPenalty = 2,

    /// <summary>Descuento por tiempo expirado.</summary>
    TimePenalty = 3
}