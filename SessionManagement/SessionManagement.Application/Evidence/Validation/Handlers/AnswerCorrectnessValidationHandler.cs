namespace SessionManagement.Application.Evidence.Validation.Handlers;

/// <summary>
/// RN-12: evalúa corrección de la respuesta sin mutar el agregado.
/// </summary>
public sealed class AnswerCorrectnessValidationHandler : EvidenceValidationHandlerBase
{
    protected override void Validate(EvidenceValidationContext context)
    {
        if (context.CurrentRule is null)
            return;

        context.IsCorrect = string.Equals(
            context.Payload.Trim(),
            context.CurrentRule.ExpectedValue.Trim(),
            StringComparison.OrdinalIgnoreCase);

        if (!context.IsCorrect)
            context.RejectionReason = "Respuesta/código incorrecto.";
    }
}
