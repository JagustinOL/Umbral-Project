using System.ComponentModel.DataAnnotations;

namespace Umbral.Shared.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    [Required]
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string ExchangeName { get; init; } = "umbral.domain.events";
}
