using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;

namespace ScreenProducerAPI.Services;

public class ReorderService
{
    private readonly TargetQuantityService _targetQuantityService;
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly PurchaseOrderQueueService _queueService;
    private readonly MaterialService _materialService;
    private readonly ILogger<ReorderService> _logger;
    private readonly IOptionsMonitor<TargetQuantitiesConfig> _targetConfig;
    private readonly IOptionsMonitor<ReorderSettingsConfig> _reorderConfig;

    public ReorderService(
        TargetQuantityService targetQuantityService,
        PurchaseOrderService purchaseOrderService,
        PurchaseOrderQueueService queueService,
        MaterialService materialService,
        ILogger<ReorderService> logger,
        IOptionsMonitor<TargetQuantitiesConfig> targetConfig,
        IOptionsMonitor<ReorderSettingsConfig> reorderConfig)
    {
        _targetQuantityService = targetQuantityService;
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
        _targetConfig = targetConfig;
        _reorderConfig = reorderConfig;
        _materialService = materialService;
        _queueService = queueService;
    }

    public async Task<ReorderResult> CheckAndProcessReordersAsync()
    {
        if (!_reorderConfig.CurrentValue.EnableAutoReorder)
        {
            _logger.LogInformation("Auto reorder is disabled");
            return new ReorderResult { AutoReorderEnabled = false };
        }

        _logger.LogInformation("Checking inventory levels for reorder requirements");

        var status = await _targetQuantityService.GetInventoryStatusAsync();
        var config = _targetConfig.CurrentValue;
        var result = new ReorderResult { AutoReorderEnabled = true };

        // Check sand reorder
        if (status.Sand.NeedsReorder)
        {
            _logger.LogInformation("Sand reorder required. Current + Incoming: {Total}, Reorder Point: {ReorderPoint}",
                status.Sand.Total, status.Sand.ReorderPoint);

            var sandOrder = await CreateMaterialReorderAsync("sand", config.Sand.OrderQuantity);
            if (sandOrder != null)
            {
                result.SandOrderCreated = true;
                result.SandOrderId = sandOrder.Id;
                _logger.LogInformation("Created sand reorder: Order {OrderId} for {Quantity}kg",
                    sandOrder.Id, config.Sand.OrderQuantity);
            }
        }

        // Check copper reorder
        if (status.Copper.NeedsReorder)
        {
            _logger.LogInformation("Copper reorder required. Current + Incoming: {Total}, Reorder Point: {ReorderPoint}",
                status.Copper.Total, status.Copper.ReorderPoint);

            var copperOrder = await CreateMaterialReorderAsync("copper", config.Copper.OrderQuantity);
            if (copperOrder != null)
            {
                result.CopperOrderCreated = true;
                result.CopperOrderId = copperOrder.Id;
                _logger.LogInformation("Created copper reorder: Order {OrderId} for {Quantity}kg",
                    copperOrder.Id, config.Copper.OrderQuantity);
            }
        }

        // Check equipment reorder
        if (status.Equipment.NeedsReorder)
        {
            _logger.LogInformation("Equipment reorder required. Current + Incoming: {Total}, Reorder Point: {ReorderPoint}",
                status.Equipment.Total, status.Equipment.ReorderPoint);

            var equipmentOrder = await CreateEquipmentReorderAsync(config.Equipment.OrderQuantity);
            if (equipmentOrder != null)
            {
                result.EquipmentOrderCreated = true;
                result.EquipmentOrderId = equipmentOrder.Id;
                _logger.LogInformation("Created equipment reorder: Order {OrderId} for {Quantity} units",
                    equipmentOrder.Id, config.Equipment.OrderQuantity);
            }
        }

        if (!result.SandOrderCreated && !result.CopperOrderCreated && !result.EquipmentOrderCreated)
        {
            _logger.LogInformation("No reorders required at this time");
        }

        return result;
    }

    private async Task<PurchaseOrder?> CreateMaterialReorderAsync(string materialName, int quantity)
    {
        try
        {
            // For now, use placeholder values - will be replaced with Hand integration later
            string supplierOrigin = "hand"; // need to decide
            var unitPrice = materialName.ToLower() == "sand" ? 50 : 75; // Default prices
            var sellerBankAccount = "SUPPLIER_BANK_PLACEHOLDER"; // Will come from Hand/Recycler
            Random rnd = new Random();
            var orderId = rnd.Next(100000); // Generate unique order ID

            // Find material ID
            var material = await _materialService.GetMaterialAsync(materialName);
            var materialId = material?.Id;

            var purchaseOrder = await _purchaseOrderService.CreatePurchaseOrderAsync(
                (int)orderId,
                quantity,
                unitPrice,
                sellerBankAccount,
                supplierOrigin,
                materialId,
                false
            );

            if (purchaseOrder != null)
            {
                _queueService.EnqueuePurchaseOrder(purchaseOrder.Id);
            }

            return purchaseOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {MaterialName} reorder", materialName);
            return null;
        }
    }

    private async Task<PurchaseOrder?> CreateEquipmentReorderAsync(int quantity)
    {
        try
        {
            // For now, use placeholder values - will be replaced with Hand integration later
            var unitPrice = 10000; // Default equipment price
            var sellerBankAccount = "EQUIPMENT_SUPPLIER_BANK_PLACEHOLDER"; // Will come from Hand
            Random rnd = new Random();
            var orderId = rnd.Next(100000); // Generate unique order ID

            var purchaseOrder = await _purchaseOrderService.CreatePurchaseOrderAsync(
                (int)orderId,
                quantity,
                unitPrice,
                sellerBankAccount,
                "hand",
                null,
                true
            );

            if (purchaseOrder != null)
            {
                _queueService.EnqueuePurchaseOrder(purchaseOrder.Id);
            }

            return purchaseOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating equipment reorder");
            return null;
        }
    }
}

public class ReorderResult
{
    public bool AutoReorderEnabled { get; set; }
    public bool SandOrderCreated { get; set; }
    public bool CopperOrderCreated { get; set; }
    public bool EquipmentOrderCreated { get; set; }
    public int? SandOrderId { get; set; }
    public int? CopperOrderId { get; set; }
    public int? EquipmentOrderId { get; set; }
}