using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models.Configuration;

namespace ScreenProducerAPI.Services;

public class QueueProcessingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<QueueSettingsConfig> _config;

    public QueueProcessingBackgroundService(
        IServiceProvider serviceProvider,
        IOptionsMonitor<QueueSettingsConfig> config)
    {
        _serviceProvider = serviceProvider;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                break;
            }
            catch (Exception ex)
            {
                // Continue running despite errors
            }
        }
    }
}