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
        _targetConfig = targetConfig;
        _reorderConfig = reorderConfig;
        _materialService = materialService;
        _queueService = queueService;
    }

    public async Task<ReorderResult> CheckAndProcessReordersAsync()
    {
        if (!_reorderConfig.CurrentValue.EnableAutoReorder)
        {
            return new ReorderResult { AutoReorderEnabled = false };
        }

        var status = await _targetQuantityService.GetInventoryStatusAsync();
        var config = _targetConfig.CurrentValue;
        var result = new ReorderResult { AutoReorderEnabled = true };

        if (status.Sand.NeedsReorder)
        {
            var sandOrder = await CreateMaterialReorderAsync("sand", config.Sand.OrderQuantity);
            if (sandOrder != null)
            {
                result.SandOrderCreated = true;
                result.SandOrderId = sandOrder.Id;
            }
        }

        if (status.Copper.NeedsReorder)
        {
            var copperOrder = await CreateMaterialReorderAsync("copper", config.Copper.OrderQuantity);
            if (copperOrder != null)
            {
                result.CopperOrderCreated = true;
                result.CopperOrderId = copperOrder.Id;
            }
        }

        // Check equipment reorder
        if (status.Equipment.NeedsReorder)
        {
            var equipmentOrder = await CreateEquipmentReorderAsync(config.Equipment.OrderQuantity);
            if (equipmentOrder != null)
            {
                result.EquipmentOrderCreated = true;
                result.EquipmentOrderId = equipmentOrder.Id;
            }
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