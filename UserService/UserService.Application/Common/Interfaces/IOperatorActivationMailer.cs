namespace UserService.Application.Common.Interfaces;

/// <summary>
/// Compone y envía el correo con el código de activación de operador.
/// Separado del handler CQRS para respetar SRP.
/// </summary>
public interface IOperatorActivationMailer
{
    Task SendActivationCodeAsync(
        string email,
        string firstName,
        string setupCode,
        int ttlDays,
        CancellationToken cancellationToken = default);
}
