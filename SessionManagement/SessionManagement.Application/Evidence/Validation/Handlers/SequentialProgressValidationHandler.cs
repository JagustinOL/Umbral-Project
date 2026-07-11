using SessionManagement.Application.Evidence;
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
        var currentRule = orderedRules.FirstOrDefault(rule =>
            !NodeProgressHelper.IsNodeCompleted(context.Session, context.TeamId, rule));

        if (currentRule is null)
            throw new SessionDomainException($"El equipo {context.TeamId} ya completó todos los nodos.");

        context.CurrentRule = currentRule;

        var submittedRule = orderedRules.FirstOrDefault(rule => rule.NodeId == context.NodeId);
        if (submittedRule is not null &&
            NodeProgressHelper.IsNodeCompleted(context.Session, context.TeamId, submittedRule))
        {
            throw new SessionDomainException("La etapa ya está cerrada para este equipo (RN-04).");
        }

        if (NodeProgressHelper.IsNodeCompleted(context.Session, context.TeamId, currentRule))
            throw new SessionDomainException("La etapa ya está cerrada para este equipo (RN-04).");

        if (currentRule.NodeId != context.NodeId)
            throw new SessionDomainException(
                $"Progresión secuencial inválida. Se esperaba el nodo {currentRule.NodeId} (RN-11).");

        if (currentRule.ValidationType != context.ExpectedType)
            throw new SessionDomainException("Tipo de validación no coincide con el nodo actual.");

        if (currentRule.ValidationType == Domain.ValueObjects.NodeValidationType.Trivia)
        {
            var nextQuestionIndex = NodeProgressHelper.GetNextQuestionIndex(
                context.Session,
                context.TeamId,
                context.NodeId,
                currentRule.ExpectedAnswers.Count);

            if (nextQuestionIndex >= currentRule.ExpectedAnswers.Count)
                throw new SessionDomainException("La etapa ya está cerrada para este equipo (RN-04).");

            if (context.QuestionIndex.HasValue && context.QuestionIndex.Value != nextQuestionIndex)
                throw new SessionDomainException(
                    $"Progresión secuencial inválida. Se esperaba la pregunta {nextQuestionIndex} (RN-11).");

            context.ResolvedQuestionIndex = nextQuestionIndex;
        }
        else
        {
            context.ResolvedQuestionIndex = 0;
        }
    }
}
