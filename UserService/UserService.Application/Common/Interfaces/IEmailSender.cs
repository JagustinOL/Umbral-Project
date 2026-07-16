namespace UserService.Application.Common.Interfaces;

public sealed record EmailMessage(
    string ToAddress,
    string ToName,
    string Subject,
    string PlainTextBody,
    string HtmlBody);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
