using ScoringAudit.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace ScoringAudit.Domain.ValueObjects;

/// <summary>
/// Value Object que encapsula el motivo de una penalización.
///
/// RB-06: Toda penalización debe registrar motivo y momento de aplicación.
/// Este VO garantiza que el motivo nunca sea vacío ni nulo — si no hay
/// motivo, no existe PenaltyReason y por ende no puede crearse el ScoreEntry.
///
/// La inmutabilidad del record garantiza que el historial de penalizaciones
/// nunca pueda alterarse retroactivamente (RB-07 — trazabilidad de origen).
/// </summary>
public sealed record PenaltyReason
{
    public string Description { get; }

    /// <summary>
    /// Categoría de la penalización para facilitar reportes de auditoría.
    /// Ej: "ManualOperator", "HintUsage", "TimeExpired".
    /// </summary>
    public PenaltyCategory Category { get; }
    public Guid? AppliedByOperatorId { get; }

    [JsonConstructor]
    private PenaltyReason(string description, PenaltyCategory category, Guid? appliedByOperatorId = null)
    {
        Description = description;
        Category = category;
        AppliedByOperatorId = appliedByOperatorId;
    }

    /// <summary>
    /// Crea un PenaltyReason para una penalización manual del Operador.
    /// RB-06: el motivo es obligatorio — lanza excepción si está vacío.
    /// </summary>
    public static PenaltyReason ForManualPenalty(string description, Guid? appliedByOperatorId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ScoringDomainException(
                "El motivo de la penalización manual es obligatorio. " +
                "El Operador debe describir la razón antes de aplicar la penalización.");

        if (description.Trim().Length < 5)
            throw new ScoringDomainException(
                "El motivo de la penalización debe tener al menos 5 caracteres " +
                "para garantizar una descripción significativa.");

        return new PenaltyReason(description.Trim(), PenaltyCategory.ManualOperator, appliedByOperatorId);
    }

    /// <summary>Crea un PenaltyReason para el uso de una pista.</summary>
    public static PenaltyReason ForHintUsage(Guid hintId, int order) =>
        new($"Pista #{order} utilizada (HintId: {hintId})", PenaltyCategory.HintUsage);

    /// <summary>Crea un PenaltyReason para expiración de tiempo.</summary>
    public static PenaltyReason ForTimeExpired() =>
        new("Penalización por tiempo expirado.", PenaltyCategory.TimeExpired);

    public override string ToString() => $"[{Category}] {Description}";
}