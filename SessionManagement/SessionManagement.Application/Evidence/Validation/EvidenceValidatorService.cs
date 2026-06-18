namespace SessionManagement.Application.Evidence.Validation;

/// <summary>
/// Patrón Chain of Responsibility — compone y ejecuta la cadena de validaciones
/// previas al procesamiento de evidencias.
/// </summary>
public sealed class EvidenceValidatorService
{
    private readonly IEvidenceValidationHandler _chain;

    public EvidenceValidatorService(
        Handlers.SessionActiveValidationHandler sessionActive,
        Handlers.TeamRegisteredValidationHandler teamRegistered,
        Handlers.NodeAllowedValidationHandler nodeAllowed,
        Handlers.SequentialProgressValidationHandler sequentialProgress,
        Handlers.AnswerCorrectnessValidationHandler answerCorrectness)
    {
        sessionActive
            .SetNext(teamRegistered)
            .SetNext(nodeAllowed)
            .SetNext(sequentialProgress)
            .SetNext(answerCorrectness);

        _chain = sessionActive;
    }

    public void Validate(EvidenceValidationContext context) => _chain.Handle(context);
}
