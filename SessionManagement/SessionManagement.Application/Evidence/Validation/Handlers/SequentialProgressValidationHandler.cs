using SessionManagement.Domain.Exceptions;

namespace SessionManagement.Application.Evidence.Validation.Handlers;

/// <summary>
/// RN-04 y RN-11: progresión secuencial y nodo no cerrado.
/// </summary>
public sealed class SequentialProgressValidationHandler : EvidenceValidationHandlerBase
{
    protected override void Validate(EvidenceValidationContext context)
    {
        var orderedRules = context.ValidationRules.OrderBy(x => x.ExecutionOrder).ToList();
        var completedNodeIds = context.Session.EvidenceSubmissions
            .Where(e => e.TeamId == context.TeamId && e.IsValid == true)
            .Select(e => e.MissionNodeId)
            .Distinct()
            .ToHashSet();

        var currentRule = orderedRules.FirstOrDefault(x => !completedNodeIds.Contains(x.NodeId));
        if (currentRule is null)
            throw new SessionDomainException($"El equipo {context.TeamId} ya completó todos los nodos.");

        context.CurrentRule = currentRule;

        var alreadyClosed = context.Session.EvidenceSubmissions.Any(e =>
            e.TeamId == context.TeamId &&
            e.MissionNodeId == context.NodeId &&
            e.IsValid == true);

        if (alreadyClosed)
            throw new SessionDomainException("La etapa ya está cerrada para este equipo (RN-04).");

        if (currentRule.NodeId != context.NodeId)
            throw new SessionDomainException(
                $"Progresión secuencial inválida. Se esperaba el nodo {currentRule.NodeId} (RN-11).");

        if (currentRule.ValidationType != context.ExpectedType)
            throw new SessionDomainException("Tipo de validación no coincide con el nodo actual.");
    }
}
