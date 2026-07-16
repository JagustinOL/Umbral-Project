namespace UserService.Application.Common;

/// <summary>
/// Resultado expuesto al API tras crear un operador (sin el código en claro).
/// </summary>
public sealed record CreateOperatorResult(
    Guid OperatorId,
    string Email,
    bool ActivationEmailSent);

/// <summary>
/// Credenciales temporales generadas en Keycloak para enviar por correo.
/// </summary>
public sealed record OperatorSetupCredentials(
    Guid OperatorId,
    string Email,
    string FirstName,
    string LastName,
    string SetupCode,
    int TtlDays);

/// <summary>
/// Resultado de regenerar/reenviar el código de activación.
/// </summary>
public sealed record ResendOperatorActivationResult(
    Guid OperatorId,
    string Email,
    bool ActivationEmailSent);
