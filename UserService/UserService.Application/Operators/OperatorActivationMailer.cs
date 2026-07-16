using System.Net;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Operators;

public sealed class OperatorActivationMailer : IOperatorActivationMailer
{
    private readonly IEmailSender _emailSender;

    public OperatorActivationMailer(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendActivationCodeAsync(
        string email,
        string firstName,
        string setupCode,
        int ttlDays,
        CancellationToken cancellationToken = default)
    {
        var safeName = string.IsNullOrWhiteSpace(firstName) ? "operador" : firstName.Trim();
        var subject = "UMBRAL — Código de activación de operador";

        var plain =
            $"Hola {safeName},\n\n" +
            "Se creó (o reactivó) tu cuenta de operador en UMBRAL.\n\n" +
            "================================\n" +
            $"  CÓDIGO DE ACTIVACIÓN: {setupCode}\n" +
            "================================\n\n" +
            $"Este código expira en {ttlDays} día(s).\n\n" +
            "Cómo activar:\n" +
            "1. Abre la pantalla de acceso de UMBRAL\n" +
            "2. Elige \"Activar cuenta\"\n" +
            "3. Ingresa tu correo, este código y una contraseña nueva\n\n" +
            "Si no solicitaste esta cuenta, ignora este mensaje.\n\n" +
            "— Equipo UMBRAL\n";

        var encodedName = WebUtility.HtmlEncode(safeName);
        var encodedCode = WebUtility.HtmlEncode(setupCode);
        var html =
            "<!DOCTYPE html><html><body style=\"font-family:Segoe UI,Arial,sans-serif;line-height:1.5;color:#111;\">" +
            $"<p>Hola <strong>{encodedName}</strong>,</p>" +
            "<p>Se creó (o reactivó) tu cuenta de operador en <strong>UMBRAL</strong>.</p>" +
            "<p>Tu código de activación es:</p>" +
            "<p style=\"font-size:28px;letter-spacing:0.25em;font-family:Consolas,monospace;" +
            "background:#f4f4f5;border:1px solid #d4d4d8;border-radius:8px;padding:16px 20px;" +
            $"display:inline-block;\"><strong>{encodedCode}</strong></p>" +
            $"<p>El código expira en <strong>{ttlDays}</strong> día(s).</p>" +
            "<ol>" +
            "<li>Abre la pantalla de acceso de UMBRAL</li>" +
            "<li>Elige <em>Activar cuenta</em></li>" +
            "<li>Ingresa tu correo, este código y una contraseña nueva</li>" +
            "</ol>" +
            "<p>Si no solicitaste esta cuenta, ignora este mensaje.</p>" +
            "<p>— Equipo UMBRAL</p>" +
            "</body></html>";

        return _emailSender.SendAsync(
            new EmailMessage(email, safeName, subject, plain, html),
            cancellationToken);
    }
}
