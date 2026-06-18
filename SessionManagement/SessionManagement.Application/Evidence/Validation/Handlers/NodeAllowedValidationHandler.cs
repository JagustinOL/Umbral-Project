using SessionManagement.Domain.Exceptions;

namespace SessionManagement.Application.Evidence.Validation.Handlers;

public sealed class NodeAllowedValidationHandler : EvidenceValidationHandlerBase
{
    protected override void Validate(EvidenceValidationContext context)
    {
        var allowed = context.Session.AllowedNodes.Any(n => n.NodeId == context.NodeId);
        if (!allowed)
            throw new SessionDomainException(
                $"El nodo {context.NodeId} no pertenece a los nodos permitidos de esta sesión (RB-05).");
    }
}
