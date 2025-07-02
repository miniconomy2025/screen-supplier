using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models.Configuration;

namespace ScreenProducerAPI.Services;

public class QueueProcessingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueProcessingBackgroundService> _logger;
    private readonly IOptionsMonitor<QueueSettingsConfig> _config;

    public QueueProcessingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<QueueProcessingBackgroundService> logger,
        IOptionsMonitor<QueueSettingsConfig> config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queue processing background service started");

        // Populate queue from database on startup
        using (var scope = _serviceProvider.CreateScope())
        {
            var queueService = scope.ServiceProvider.GetRequiredService<PurchaseOrderQueueService>();
            await queueService.PopulateQueueFromDatabaseAsync();
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var intervalSeconds = _config.CurrentValue.ProcessingIntervalSeconds;
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<PurchaseOrderQueueService>();

                await queueService.ProcessQueueAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Queue processing background service cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in queue processing background service");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("Queue processing background service stopped");
    }
}