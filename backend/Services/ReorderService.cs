using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services.BankServices;

namespace ScreenProducerAPI.Services;

public class ReorderService
{
    private readonly TargetQuantityService _targetQuantityService;
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly PurchaseOrderQueueService _queueService;
    private readonly ProductService _productService;
    private readonly MaterialService _materialService;
    private readonly BankService _bankService;
    private readonly IOptionsMonitor<TargetQuantitiesConfig> _targetConfig;
    private readonly IOptionsMonitor<ReorderSettingsConfig> _reorderConfig;

    public ReorderService(
        TargetQuantityService targetQuantityService,
        PurchaseOrderService purchaseOrderService,
        PurchaseOrderQueueService queueService,
        ProductService productService,
        MaterialService materialService,
        BankService bankService,
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
        _productService = productService;
        _bankService = bankService;
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

        if (_reorderConfig.CurrentValue.EnableScreenStockCheck)
        {
            var screensInStock = await _productService.GetAvailableStockAsync();

            if (screensInStock >= _reorderConfig.CurrentValue.MaxScreensBeforeStopOrdering)
            {
                return result;
            }
        }

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
            // Logic to ping both hand and recycler - buy from cheapest
            // get back price need to check bank balance
            var availableBankBalance = await _bankService.GetAccountBalanceAsync();

            // For now, use placeholder values - will be replaced with Hand integration later
            string supplierOrigin = "hand"; // need to decide
            var unitPrice = materialName.ToLower() == "sand" ? 50 : 75; // Default prices
            var sellerBankAccount = "SUPPLIER_BANK_PLACEHOLDER"; // Will come from Hand/Recycler
            Random rnd = new Random();
            var orderId = rnd.Next(100000); // Generate unique order ID


            // not enough monies
            if (availableBankBalance < unitPrice *  quantity)
            {
                return null;
            }

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
            var availableBankBalance = await _bankService.GetAccountBalanceAsync();

            // For now, use placeholder values - will be replaced with Hand integration later
            var unitPrice = 10000; // Default equipment price
            var sellerBankAccount = "EQUIPMENT_SUPPLIER_BANK_PLACEHOLDER"; // Will come from Hand
            Random rnd = new Random();
            var orderId = rnd.Next(100000); // Generate unique order ID

            // probably will change to total order price or something like that
            if (availableBankBalance < unitPrice * quantity)
            {
                return null;
            }

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