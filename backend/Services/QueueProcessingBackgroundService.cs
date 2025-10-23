using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models.Configuration;

namespace ScreenProducerAPI.Services;

public class QueueProcessingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<QueueSettingsConfig> _config;
    private readonly ILogger<QueueProcessingBackgroundService> _logger;

    public QueueProcessingBackgroundService(
        IServiceProvider serviceProvider,
        IOptionsMonitor<QueueSettingsConfig> config,
        ILogger<QueueProcessingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        using (var scope = _serviceProvider.CreateScope())
        {
            var queueService = scope.ServiceProvider.GetRequiredService<IPurchaseOrderQueueService>();
            _logger.LogInformation("QueueProcessingBackgroundService: Calling populate queue");
            await queueService.PopulateQueueFromDatabaseAsync();
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var intervalSeconds = _config.CurrentValue.ProcessingIntervalSeconds;
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<IPurchaseOrderQueueService>();

                await queueService.ProcessQueueAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Continue running despite errors
            }
        }
    }
}