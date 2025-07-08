using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ScreenProducerAPI.Services;

public class PurchaseOrderQueueService
{
    private readonly ConcurrentQueue<QueueItem> _queue = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PurchaseOrderQueueService> _logger;
    private readonly IOptionsMonitor<QueueSettingsConfig> _queueConfig;
    private readonly IOptionsMonitor<CompanyInfoConfig> _companyConfig;

    public PurchaseOrderQueueService(
        IServiceProvider serviceProvider,
        ILogger<PurchaseOrderQueueService> logger,
        IOptionsMonitor<QueueSettingsConfig> queueConfig,
        IOptionsMonitor<CompanyInfoConfig> companyConfig)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queueConfig = queueConfig;
        _companyConfig = companyConfig;
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

        // Process all items currently in queue
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

                var processed = await ProcessPurchaseOrderAsync(scope.ServiceProvider, purchaseOrder, item);

                if (processed)
                {
                    processedCount++;
                    _logger.LogInformation("Successfully processed purchase order {PurchaseOrderId} in status {Status}",
                        purchaseOrder.Id, purchaseOrder.OrderStatus.Status);
                }
                else
                {
                    // Processing failed, handle retry
                    item.RetryCount++;
                    item.LastProcessed = DateTime.UtcNow;

                    if (item.RetryCount >= maxRetries)
                    {
                        _logger.LogWarning("Purchase order {PurchaseOrderId} exceeded max retries ({MaxRetries}), abandoning",
                            item.PurchaseOrderId, maxRetries);

                        await AbandonPurchaseOrderAsync(scope.ServiceProvider, purchaseOrder);
                    }
                    else
                    {
                        _logger.LogInformation("Purchase order {PurchaseOrderId} failed processing, retry {RetryCount}/{MaxRetries}",
                            item.PurchaseOrderId, item.RetryCount, maxRetries);

                        // Add back to queue for retry
                        _queue.Enqueue(item);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue item for purchase order {PurchaseOrderId}", item.PurchaseOrderId);

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
                }
            }
        }

        if (processedCount > 0)
        {
            _logger.LogInformation("Queue processing completed. Processed {ProcessedCount} orders. Remaining in queue: {RemainingCount}",
                processedCount, _queue.Count);
        }
    }

    private async Task<bool> ProcessPurchaseOrderAsync(IServiceProvider serviceProvider, PurchaseOrder purchaseOrder, QueueItem queueItem)
    {
        var status = purchaseOrder.OrderStatus.Status;

        switch (status)
        {
            case Status.RequiresPaymentToSupplier:
                return await ProcessSupplierPaymentAsync(serviceProvider, purchaseOrder, queueItem);

            case Status.RequiresDelivery:
                return await ProcessShippingRequestAsync(serviceProvider, purchaseOrder, queueItem);

            case Status.RequiresPaymentToLogistics:
                return await ProcessLogisticsPaymentAsync(serviceProvider, purchaseOrder, queueItem);

            case Status.WaitingForDelivery:
                _logger.LogInformation("Purchase order {PurchaseOrderId} is waiting for delivery, removing from queue", purchaseOrder.Id);
                return true;

            case Status.Delivered:
            case Status.Abandoned:
                _logger.LogInformation("Purchase order {PurchaseOrderId} is in terminal state {Status}, removing from queue",
                    purchaseOrder.Id, status);
                return true;

            default:
                _logger.LogWarning("Purchase order {PurchaseOrderId} has unexpected status {Status}, removing from queue",
                    purchaseOrder.Id, status);
                return true;
        }
    }

    private async Task<bool> ProcessSupplierPaymentAsync(IServiceProvider serviceProvider, PurchaseOrder purchaseOrder, QueueItem queueItem)
    {
        try
        {
            _logger.LogInformation("Processing supplier payment for purchase order {PurchaseOrderId}", purchaseOrder.Id);

            var bankService = serviceProvider.GetRequiredService<BankService>();
            var purchaseOrderService = serviceProvider.GetRequiredService<PurchaseOrderService>();

            var totalAmount = purchaseOrder.Quantity * purchaseOrder.UnitPrice;
            var description = purchaseOrder.OrderID.ToString(); // Use OrderID as description

            var paymentSuccess = await bankService.MakePaymentAsync(
                purchaseOrder.BankAccountNumber,
                "commercial-bank", // Use enum value from bank API
                totalAmount,
                description);

            if (paymentSuccess)
            {
                // Update status to requires_delivery
                await purchaseOrderService.UpdateStatusAsync(purchaseOrder.Id, Status.RequiresDelivery);

                // Add back to queue for next step
                EnqueuePurchaseOrder(purchaseOrder.Id);

                _logger.LogInformation("Supplier payment successful for purchase order {PurchaseOrderId}, amount {Amount}",
                    purchaseOrder.Id, totalAmount);
                return true;
            }
            else
            {
                queueItem.LastError = "Supplier payment failed";
                _logger.LogWarning("Supplier payment failed for purchase order {PurchaseOrderId}", purchaseOrder.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            queueItem.LastError = ex.Message;
            _logger.LogError(ex, "Error processing supplier payment for purchase order {PurchaseOrderId}", purchaseOrder.Id);
            return false;
        }
    }

    private async Task<bool> ProcessShippingRequestAsync(IServiceProvider serviceProvider, PurchaseOrder purchaseOrder, QueueItem queueItem)
    {
        try
        {
            _logger.LogInformation("Processing shipping request for purchase order {PurchaseOrderId}", purchaseOrder.Id);

            var logisticsService = serviceProvider.GetRequiredService<LogisticsService>();
            var purchaseOrderService = serviceProvider.GetRequiredService<PurchaseOrderService>();
            var equipmentParamService = serviceProvider.GetRequiredService<EquipmentService>();
            var companyInfo = _companyConfig.CurrentValue;

            // Create pickup items based on order type
            var pickupItems = new List<PickupRequestItem>();

            var equipmentToGet = await equipmentParamService.GetEquipmentParametersAsync();
            if (equipmentToGet == null)
            {
                _logger.LogError("Failed to get weight of machine for request");
                return false;
            }

            if (purchaseOrder.EquipmentOrder == true)
            {
                // Treated as weight here...
                pickupItems = LogisticsService.CreatePickupItems("equipment", equipmentToGet.EquipmentWeight, true);
            }
            else if (purchaseOrder.RawMaterial != null)
            {
                pickupItems = LogisticsService.CreatePickupItems(purchaseOrder.RawMaterial.Name, purchaseOrder.Quantity, false);
            }
            else
            {
                queueItem.LastError = "Invalid purchase order configuration";
                _logger.LogError("Purchase order {PurchaseOrderId} has invalid configuration", purchaseOrder.Id);
                return false;
            }

            var (pickupRequestId, logisticsBankAccount, shippingPrice) = await logisticsService.RequestPickupAsync(
                purchaseOrder.Origin,
                companyInfo.CompanyId,
                purchaseOrder.OrderID.ToString(),
                pickupItems
            );

            // Update shipment ID and status
            await purchaseOrderService.UpdateShipmentIdAsync(purchaseOrder.Id, int.Parse(pickupRequestId));
            await purchaseOrderService.UpdateStatusAsync(purchaseOrder.Id, Status.RequiresPaymentToLogistics);
            await purchaseOrderService.UpdateOrderShippingDetailsAsync(purchaseOrder.Id, logisticsBankAccount, shippingPrice);

            // Add back to queue for logistics payment
            EnqueuePurchaseOrder(purchaseOrder.Id);

            _logger.LogInformation("Shipping request successful for purchase order {PurchaseOrderId}, pickup request {PickupRequestId}",
                purchaseOrder.Id, pickupRequestId);
            return true;
        }
        catch (Exception ex)
        {
            queueItem.LastError = ex.Message;
            _logger.LogError(ex, "Error processing shipping request for purchase order {PurchaseOrderId}", purchaseOrder.Id);
            return false;
        }
    }

    private async Task<bool> ProcessLogisticsPaymentAsync(IServiceProvider serviceProvider, PurchaseOrder purchaseOrder, QueueItem queueItem)
    {
        try
        {
            _logger.LogInformation("Processing logistics payment for purchase order {PurchaseOrderId}", purchaseOrder.Id);

            var bankService = serviceProvider.GetRequiredService<BankService>();
            var purchaseOrderService = serviceProvider.GetRequiredService<PurchaseOrderService>();

            var description = $"{purchaseOrder.ShipmentID}";

            var paymentSuccess = await bankService.MakePaymentAsync(
                purchaseOrder.ShipperBankAccout,
                "commercial-bank",
                purchaseOrder.OrderShippingPrice,
                description);

            if (paymentSuccess)
            {
                // Update status to waiting_delivery
                await purchaseOrderService.UpdateStatusAsync(purchaseOrder.Id, Status.WaitingForDelivery);

                _logger.LogInformation("Logistics payment successful for purchase order {PurchaseOrderId}, amount {Amount}",
                    purchaseOrder.Id, purchaseOrder.OrderShippingPrice);
                return true;
            }
            else
            {
                queueItem.LastError = "Logistics payment failed";
                _logger.LogWarning("Logistics payment failed for purchase order {PurchaseOrderId}", purchaseOrder.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            queueItem.LastError = ex.Message;
            _logger.LogError(ex, "Error processing logistics payment for purchase order {PurchaseOrderId}", purchaseOrder.Id);
            return false;
        }
    }

    private async Task AbandonPurchaseOrderAsync(IServiceProvider serviceProvider, PurchaseOrder purchaseOrder)
    {
        try
        {
            var purchaseOrderService = serviceProvider.GetRequiredService<PurchaseOrderService>();
            await purchaseOrderService.UpdateStatusAsync(purchaseOrder.Id, Status.Abandoned);

            _logger.LogWarning("Purchase order {PurchaseOrderId} has been abandoned after max retries", purchaseOrder.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abandoning purchase order {PurchaseOrderId}", purchaseOrder.Id);
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

    public int GetQueueCount()
    {
        return _queue.Count;
    }
}