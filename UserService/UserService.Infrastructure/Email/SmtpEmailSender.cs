using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Application.Common.Interfaces;
using UserService.Application.Exceptions;

namespace UserService.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_options.Enabled)
        {
            _logger.LogWarning(
                "Email deshabilitado (Email:Enabled=false). No se envió correo a {To}. Asunto: {Subject}",
                message.ToAddress,
                message.Subject);
            return;
        }

        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = message.Subject,
                // multipart/alternative: no usar Body+IsBodyHtml a la vez (rompe el texto plano en MailHog).
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            mail.To.Add(new MailAddress(message.ToAddress, message.ToName));

            var plainView = AlternateView.CreateAlternateViewFromString(
                message.PlainTextBody,
                Encoding.UTF8,
                MediaTypeNames.Text.Plain);
            var htmlView = AlternateView.CreateAlternateViewFromString(
                message.HtmlBody,
                Encoding.UTF8,
                MediaTypeNames.Text.Html);

            mail.AlternateViews.Add(plainView);
            mail.AlternateViews.Add(htmlView);

            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = true
            };

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(mail, cancellationToken);

            _logger.LogInformation("Correo enviado a {To} ({Subject}).", message.ToAddress, message.Subject);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException)
        {
            _logger.LogError(ex, "Fallo al enviar correo a {To}.", message.ToAddress);
            throw new ExternalDependencyException(
                $"No fue posible enviar el correo de activación a '{message.ToAddress}'.",
                ex);
        }
    }
}
