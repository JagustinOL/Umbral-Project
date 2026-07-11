using SessionManagement.Application.Evidence;
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
            ValidationRules = request.ValidationRules,
            QuestionIndex = request.QuestionIndex
        };

        _validator.Validate(context);

        var questionIndex = ExpectedType == NodeValidationType.Trivia
            ? (int?)context.ResolvedQuestionIndex
            : null;

        var evidence = session.AcceptEvidence(
            request.TeamId,
            request.NodeId,
            normalizedPayload,
            questionIndex);

        var nodeCompleted = false;
        if (context.IsCorrect)
        {
            nodeCompleted = context.CurrentRule is not null &&
                (ExpectedType == NodeValidationType.TreasureHunt ||
                 context.ResolvedQuestionIndex >= context.CurrentRule.ExpectedAnswers.Count - 1);

            session.MarkEvidenceAsValid(evidence.Id, publishScoreEvent: nodeCompleted);
        }
        else
        {
            session.MarkEvidenceAsInvalid(evidence.Id, context.RejectionReason ?? "Respuesta incorrecta.");
        }

        return BuildResult(session, request, context, nodeCompleted);
    }

    protected virtual string NormalizePayload(string payload) => payload.Trim();

    protected virtual SubmissionResult BuildResult(
        LiveSession session,
        EvidenceSubmissionRequest request,
        EvidenceValidationContext context,
        bool nodeCompleted)
    {
        var orderedRules = request.ValidationRules.OrderBy(x => x.ExecutionOrder).ToList();
        var nextRule = orderedRules.FirstOrDefault(rule =>
            !NodeProgressHelper.IsNodeCompleted(session, request.TeamId, rule));
        var baseScore = session.AllowedNodes.FirstOrDefault(x => x.NodeId == request.NodeId)?.BaseScore ?? 0;
        var totalQuestions = context.CurrentRule?.ExpectedAnswers.Count ?? 1;

        return new SubmissionResult(
            IsCorrect: context.IsCorrect,
            CurrentNodeId: request.NodeId,
            NextNodeId: nodeCompleted ? nextRule?.NodeId : request.NodeId,
            AwardedPoints: nodeCompleted ? baseScore : 0,
            AnsweredQuestionIndex: context.ResolvedQuestionIndex,
            TotalQuestions: totalQuestions,
            NodeCompleted: nodeCompleted);
    }
}
