using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SessionManagement.Application.Common.Interfaces;

namespace SessionManagement.Infrastructure.Messaging;

public sealed class SessionScoreUpdateRabbitMqConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionScoreUpdateRabbitMqConsumer> _logger;
    private const string QueueName = "session-management.score-updates";
    private const string RoutingKey = "scoring.team.score.updated";

    public SessionScoreUpdateRabbitMqConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<SessionScoreUpdateRabbitMqConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error en consumidor RabbitMQ {Queue}. Reintentando en 5s.", QueueName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: _options.ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(args.Body.ToArray());
                var envelope = JsonSerializer.Deserialize<DomainEventEnvelope>(json, JsonOptions);
                if (envelope is not null &&
                    envelope.EventType is nameof(TeamScoreUpdatedIntegrationEvent) or "TeamScoreUpdatedIntegrationEvent")
                {
                    var message = JsonSerializer.Deserialize<TeamScoreUpdatedIntegrationEvent>(
                        envelope.Payload,
                        JsonOptions);
                    if (message is not null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var notifier = scope.ServiceProvider.GetRequiredService<ILiveSessionRealtimeNotifier>();
                        await notifier.NotifyScoreUpdateAsync(
                            message.SessionId,
                            message.TeamId,
                            message.NewTotalScore,
                            message.Ranking,
                            stoppingToken);
                    }
                }

                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando score update en {Queue}", QueueName);
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
