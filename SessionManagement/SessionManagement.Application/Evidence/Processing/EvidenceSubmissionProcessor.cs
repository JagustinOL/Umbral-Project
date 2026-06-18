using SessionManagement.Application.Evidence.Validation;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.Evidence.Processing;

/// <summary>
/// Patrón Template Method — define el flujo base de procesamiento de evidencias
/// con hooks para variantes por tipo de nodo.
/// </summary>
public abstract class EvidenceSubmissionProcessor
{
    private readonly EvidenceValidatorService _validator;

    protected EvidenceSubmissionProcessor(EvidenceValidatorService validator)
    {
        _validator = validator;
    }

    protected abstract NodeValidationType ExpectedType { get; }

    public SubmissionResult Process(LiveSession session, EvidenceSubmissionRequest request)
    {
        var normalizedPayload = NormalizePayload(request.Payload);
        var context = new EvidenceValidationContext
        {
            Session = session,
            TeamId = request.TeamId,
            NodeId = request.NodeId,
            Payload = normalizedPayload,
            ExpectedType = ExpectedType,
            ValidationRules = request.ValidationRules
        };

        _validator.Validate(context);

        var evidence = session.AcceptEvidence(request.TeamId, request.NodeId, normalizedPayload);

        if (context.IsCorrect)
            session.MarkEvidenceAsValid(evidence.Id);
        else
            session.MarkEvidenceAsInvalid(evidence.Id, context.RejectionReason ?? "Respuesta incorrecta.");

        return BuildResult(session, request, context);
    }

    protected virtual string NormalizePayload(string payload) => payload.Trim();

    protected virtual SubmissionResult BuildResult(
        LiveSession session,
        EvidenceSubmissionRequest request,
        EvidenceValidationContext context)
    {
        var orderedRules = request.ValidationRules.OrderBy(x => x.ExecutionOrder).ToList();
        var completedNodeIds = session.EvidenceSubmissions
            .Where(e => e.TeamId == request.TeamId && e.IsValid == true)
            .Select(e => e.MissionNodeId)
            .Distinct()
            .ToHashSet();

        var nextRule = orderedRules.FirstOrDefault(x => !completedNodeIds.Contains(x.NodeId));
        var baseScore = session.AllowedNodes.FirstOrDefault(x => x.NodeId == request.NodeId)?.BaseScore ?? 0;

        return new SubmissionResult(
            IsCorrect: context.IsCorrect,
            CurrentNodeId: request.NodeId,
            NextNodeId: nextRule?.NodeId,
            AwardedPoints: context.IsCorrect ? baseScore : 0);
    }
}
