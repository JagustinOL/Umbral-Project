namespace ScoringAudit.Domain.ValueObjects;

/// <summary>
/// Categorías de penalización para clasificación en auditoría.
/// Permite filtrar y agrupar ScoreEntries negativos por tipo.
/// </summary>
public enum PenaltyCategory
{
    /// <summary>Aplicada manualmente por el Operador (RB-06).</summary>
    ManualOperator = 0,

    /// <summary>Descuento automático por usar una pista (Flujo 5 del DDD).</summary>
    HintUsage = 1,

    /// <summary>Penalización por superar el tiempo máximo de la misión.</summary>
    TimeExpired = 2
}