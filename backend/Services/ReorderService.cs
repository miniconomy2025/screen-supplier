using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Services.SupplierService.Hand;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;
using ScreenProducerAPI.Services.SupplierService.Recycler;

namespace ScreenProducerAPI.Services;

public class ReorderService
{
    private readonly TargetQuantityService _targetQuantityService;
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly PurchaseOrderQueueService _queueService;
    private readonly ProductService _productService;
    private readonly MaterialService _materialService;
    private readonly BankService _bankService;
    private readonly HandService _handService;
    private readonly RecyclerService _recyclerService;
    private readonly IOptionsMonitor<TargetQuantitiesConfig> _targetConfig;
    private readonly IOptionsMonitor<ReorderSettingsConfig> _reorderConfig;

    public ReorderService(
        TargetQuantityService targetQuantityService,
        PurchaseOrderService purchaseOrderService,
        PurchaseOrderQueueService queueService,
        ProductService productService,
        MaterialService materialService,
        BankService bankService,
        HandService handService,
        RecyclerService recyclerService,
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
        _handService = handService;
        _recyclerService = recyclerService;
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
            var availableBankBalance = await _bankService.GetAccountBalanceAsync();

            // Get pricing from both Hand and Recycler services
            var (bestPrice, bestSupplier, bestBankAccount, bestOrderId) = await GetBestMaterialPriceAsync(materialName, quantity, availableBankBalance);

            if (bestOrderId == -1)
            {
                return null;
            }

            // Find material ID
            var material = await _materialService.GetMaterialAsync(materialName);
            var materialId = material?.Id;

            var purchaseOrder = await _purchaseOrderService.CreatePurchaseOrderAsync(
                bestOrderId,
                quantity,
                (int)bestPrice,
                bestBankAccount,
                bestSupplier,
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

    private async Task<(decimal price, string supplier, string bankAccount, int orderId)> GetBestMaterialPriceAsync(string materialName, int quantity, int bankBalance)
    {
        var bestPrice = decimal.MaxValue;
        var bestSupplier = "";
        var bestBankAccount = "";
        var bestOrderId = -1;

        try
        {
            // Todo get pricing from recycler aswell
            // Get pricing from Hand service
            var handMaterials = await _handService.GetRawMaterialsForSaleAsync();
            var handMaterial = handMaterials.FirstOrDefault(m =>
                m.RawMaterialName.Equals(materialName, StringComparison.OrdinalIgnoreCase));
         

            // Not null and have enough money in account
            if (handMaterial != null && (handMaterial.PricePerKg * quantity <= bankBalance))
            {
                // Make purchase request to get order details
                var purchaseRequest = new PurchaseRawMaterialRequest
                {
                    MaterialName = handMaterial.RawMaterialName,
                    WeightQuantity = quantity
                };

                var purchaseResponse = await _handService.PurchaseRawMaterialAsync(purchaseRequest);

                bestPrice = (int)Math.Ceiling(purchaseResponse.Price/quantity);
                bestSupplier = "hand";
                bestBankAccount = purchaseResponse.BankAccount;
                bestOrderId = purchaseResponse.OrderId;
            }
        }
        catch (Exception ex)
        {
            return (0, "", "", -1);
        }

        return (bestPrice, bestSupplier, bestBankAccount, bestOrderId);
    }

    private async Task<PurchaseOrder?> CreateEquipmentReorderAsync(int quantity)
    {
        try
        {
            var availableBankBalance = await _bankService.GetAccountBalanceAsync();

            // Get equipment pricing from Hand service
            var (equipmentPrice, bankAccount, orderId) = await GetEquipmentPriceAsync(quantity, availableBankBalance);

            if (orderId == -1)
            {
                return null;
            }
            
            var purchaseOrder = await _purchaseOrderService.CreatePurchaseOrderAsync(
                orderId,
                quantity,
                (int)equipmentPrice,
                bankAccount,
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

    private async Task<(decimal price, string bankAccount, int orderId)> GetEquipmentPriceAsync(int quantity, int bankAccount)
    {
        try
        {
            // Get machine details from Hand
            var machinesResponse = await _handService.GetMachinesForSaleAsync();
            var screenMachine = machinesResponse.Machines.FirstOrDefault(m => m.MachineName == "screen_machine");

            if (screenMachine == null || screenMachine.Quantity < quantity || screenMachine.Price * quantity > bankAccount)
            {
                return (0, "", -1);
            }

            // Make purchase request to get pricing and order details
            var purchaseRequest = new PurchaseMachineRequest
            {
                MachineName = "screen_machine",
                Quantity = quantity
            };

            var purchaseResponse = await _handService.PurchaseMachineAsync(purchaseRequest);

            return ((int)Math.Ceiling(purchaseResponse.Price/quantity), purchaseResponse.BankAccount, purchaseResponse.OrderId);
        }
        catch (Exception ex)
        {
            return (0, "", -1);
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