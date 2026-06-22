using UserService.Infrastructure.External.Keycloak;

namespace UserService.WebApi.Hosting;

public sealed class KeycloakBootstrapHostedService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakOptions _options;
    private readonly ILogger<KeycloakWebClientInitializer> _logger;

    public KeycloakBootstrapHostedService(
        IHttpClientFactory httpClientFactory,
        Microsoft.Extensions.Options.IOptions<KeycloakOptions> options,
        ILogger<KeycloakWebClientInitializer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(KeycloakWebClientInitializer));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await KeycloakWebClientInitializer.EnsureConfiguredAsync(
                    httpClient,
                    _options,
                    _logger,
                    stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Keycloak OIDC bootstrap failed. Retrying in 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            // Re-sync tras reinicios de Keycloak (datos en memoria en start-dev sin volumen).
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
