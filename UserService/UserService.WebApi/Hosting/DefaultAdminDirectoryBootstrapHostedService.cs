using UserService.Infrastructure.Sync;

namespace UserService.WebApi.Hosting;

public sealed class DefaultAdminDirectoryBootstrapHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DefaultAdminDirectoryBootstrapHostedService> _logger;

    public DefaultAdminDirectoryBootstrapHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DefaultAdminDirectoryBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<DefaultAdminDirectorySync>();
                if (await sync.TrySyncAsync(stoppingToken))
                    return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Default admin directory bootstrap failed. Retrying in 5s…");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
