using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScreenProducerAPI.Commands;
using ScreenProducerAPI.Commands.Queue;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Util;
using System.Collections.Concurrent;

namespace ScreenProducerAPI.Services;

public class PurchaseOrderQueueService
{
    private readonly ConcurrentQueue<QueueItem> _queue = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PurchaseOrderQueueService> _logger;
    private readonly IOptionsMonitor<QueueSettingsConfig> _queueConfig;

    public PurchaseOrderQueueService(
        IServiceProvider serviceProvider,
        ILogger<PurchaseOrderQueueService> logger,
        IOptionsMonitor<QueueSettingsConfig> queueConfig)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queueConfig = queueConfig;
    }

    public void EnqueuePurchaseOrder(int purchaseOrderId)
    {
        var queueItem = new QueueItem(purchaseOrderId);
        _queue.Enqueue(queueItem);
        _logger.LogInformation("Enqueued purchase order {PurchaseOrderId} for processing", purchaseOrderId);
    }

    public async Task ProcessQueueAsync()
    {
        if (!_queueConfig.CurrentValue.EnableQueueProcessing)
        {
            return;
        }

        var processedCount = 0;
        var maxRetries = _queueConfig.CurrentValue.MaxRetries;

        _logger.LogInformation("Processing purchase order queue. Items in queue: {QueueCount}", _queue.Count);

        var itemsToProcess = new List<QueueItem>();
        while (_queue.TryDequeue(out var item))
        {
            itemsToProcess.Add(item);
        }

        foreach (var item in itemsToProcess)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

                var purchaseOrder = await context.PurchaseOrders
                    .Include(po => po.OrderStatus)
                    .Include(po => po.RawMaterial)
                    .FirstOrDefaultAsync(po => po.Id == item.PurchaseOrderId);

                if (purchaseOrder == null)
                {
                    _logger.LogWarning("Purchase order {PurchaseOrderId} not found, removing from queue", item.PurchaseOrderId);
                    continue;
                }

                var result = await ProcessPurchaseOrderAsync(scope.ServiceProvider, purchaseOrder);

                if (result.Success)
                {
                    processedCount++;
                    _logger.LogInformation("Successfully processed purchase order {PurchaseOrderId} in status {Status}",
                        purchaseOrder.Id, purchaseOrder.OrderStatus.Status);

                    // Re-enqueue for next step if not terminal
                    if (ShouldContinueProcessing(purchaseOrder.OrderStatus.Status))
                    {
                        EnqueuePurchaseOrder(purchaseOrder.Id);
                    }
                }
                else
                {
                    HandleFailedProcessing(item, result, maxRetries);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue item for purchase order {PurchaseOrderId}", item.PurchaseOrderId);
                HandleException(item, ex, maxRetries);
            }
        }

        if (processedCount > 0)
        {
            _logger.LogInformation("Queue processing completed. Processed {ProcessedCount} orders. Remaining in queue: {RemainingCount}",
                processedCount, _queue.Count);
        }
    }

    private async Task<CommandResult> ProcessPurchaseOrderAsync(IServiceProvider serviceProvider, PurchaseOrder purchaseOrder)
    {
        var commandFactory = serviceProvider.GetRequiredService<IQueueCommandFactory>();
        var command = commandFactory.CreateCommand(purchaseOrder);
        return await command.ExecuteAsync();
    }

    private void HandleFailedProcessing(QueueItem item, CommandResult result, int maxRetries)
    {
        item.RetryCount++;
        item.LastError = result.ErrorMessage ?? "Unknown error";
        item.LastProcessed = DateTime.UtcNow;

        if (result.ShouldRetry && item.RetryCount < maxRetries)
        {
            _logger.LogInformation("Purchase order {PurchaseOrderId} failed processing, retry {RetryCount}/{MaxRetries}",
                item.PurchaseOrderId, item.RetryCount, maxRetries);
            _queue.Enqueue(item);
        }
        else
        {
            _logger.LogWarning("Purchase order {PurchaseOrderId} exceeded max retries or marked as no-retry, abandoning",
                item.PurchaseOrderId);
            _ = Task.Run(async () => await AbandonPurchaseOrderAsync(item.PurchaseOrderId));
        }
    }

    private void HandleException(QueueItem item, Exception ex, int maxRetries)
    {
        item.RetryCount++;
        item.LastError = ex.Message;
        item.LastProcessed = DateTime.UtcNow;

        if (item.RetryCount < maxRetries)
        {
            _queue.Enqueue(item);
        }
        else
        {
            _logger.LogError("Purchase order {PurchaseOrderId} exceeded max retries due to errors, abandoning", item.PurchaseOrderId);
            _ = Task.Run(async () => await AbandonPurchaseOrderAsync(item.PurchaseOrderId));
        }
    }

    private static bool ShouldContinueProcessing(string status)
    {
        return status switch
        {
            Status.RequiresPaymentToSupplier => true,
            Status.RequiresDelivery => true,
            Status.RequiresPaymentToLogistics => true,
            _ => false
        };
    }

    private async Task AbandonPurchaseOrderAsync(int purchaseOrderId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var purchaseOrderService = scope.ServiceProvider.GetRequiredService<PurchaseOrderService>();
            await purchaseOrderService.UpdateStatusAsync(purchaseOrderId, Status.Abandoned);
            _logger.LogWarning("Purchase order {PurchaseOrderId} has been abandoned", purchaseOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abandoning purchase order {PurchaseOrderId}", purchaseOrderId);
        }
    }

    public async Task PopulateQueueFromDatabaseAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

            var activeStates = new[]
            {
                Status.RequiresPaymentToSupplier,
                Status.RequiresDelivery,
                Status.RequiresPaymentToLogistics,
                Status.WaitingForDelivery
            };

            var activePurchaseOrders = await context.PurchaseOrders
                .Include(po => po.OrderStatus)
                .Where(po => activeStates.Contains(po.OrderStatus.Status))
                .Select(po => po.Id)
                .ToListAsync();

            foreach (var orderId in activePurchaseOrders)
            {
                EnqueuePurchaseOrder(orderId);
            }

            _logger.LogInformation("Populated queue with {Count} active purchase orders from database", activePurchaseOrders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating queue from database");
        }
    }

    public int GetQueueCount() => _queue.Count;
}