namespace UserService.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Si es false, no envía SMTP y solo registra en log (útil fuera de Docker).</summary>
    public bool Enabled { get; init; } = true;

    public string SmtpHost { get; init; } = "localhost";

    public int SmtpPort { get; init; } = 1025;

    public string FromAddress { get; init; } = "noreply@umbral.local";

    public string FromName { get; init; } = "UMBRAL";

    public bool UseSsl { get; init; }
}
