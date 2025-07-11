using Microsoft.Extensions.Options;
using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;
using ScreenProducerAPI.Services.SupplierService.Recycler.Models;

namespace ScreenProducerAPI.Services;

public class ReorderService
{
    private readonly TargetQuantityService _targetQuantityService;
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly PurchaseOrderQueueService _queueService;
    private readonly ProductService _productService;
    private readonly MaterialService _materialService;
    private readonly EquipmentService _equipmentService;
    private readonly BankService _bankService;
    private readonly HandService _handService;
    private readonly RecyclerService _recyclerService;
    private readonly IOptionsMonitor<TargetQuantitiesConfig> _targetConfig;
    private readonly IOptionsMonitor<ReorderSettingsConfig> _reorderConfig;
    private readonly ILogger<ReorderService> _logger;

    public ReorderService(
        TargetQuantityService targetQuantityService,
        PurchaseOrderService purchaseOrderService,
        PurchaseOrderQueueService queueService,
        ProductService productService,
        MaterialService materialService,
        EquipmentService equipmentService,
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
        _equipmentService = equipmentService;
        _bankService = bankService;
        _handService = handService;
        _recyclerService = recyclerService;
        _logger = logger;
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

        // Check screen stock limit
        if (_reorderConfig.CurrentValue.EnableScreenStockCheck)
        {
            var screensInStock = await _productService.GetAvailableStockAsync();
            if (screensInStock >= _reorderConfig.CurrentValue.MaxScreensBeforeStopOrdering)
            {
                _logger.LogInformation("Screen stock limit reached: {ScreensInStock}, skipping reorders", screensInStock);
                return result;
            }
        }

        // Check if we have at least one machine before ordering materials
        var availableEquipment = await _equipmentService.GetAllEquipmentAsync();
        var workingMachines = availableEquipment.Where(e => e.IsAvailable).Count();

        // Also check for incoming machines to prevent emergency over-ordering
        var machineStatus = await _targetQuantityService.GetInventoryStatusAsync();
        var totalMachines = workingMachines + status.Equipment.Incoming; // Available + incoming

        if (totalMachines == 0)
        {
            _logger.LogWarning("No working machines available, skipping material reorders");

            // CRITICAL: We need at least one machine to operate - use emergency ordering
            if (status.Equipment.NeedsReorder)
            {
                if (await CanAffordEmergencyMachine())
                {
                    var equipmentOrder = await CreateEquipmentReorderAsync(config.Equipment.OrderQuantity);
                    if (equipmentOrder != null)
                    {
                        result.EquipmentOrderCreated = true;
                        result.EquipmentOrderId = equipmentOrder.Id;
                        _logger.LogWarning("EMERGENCY equipment order created: {OrderId} - no working machines available", equipmentOrder.Id);
                    }
                }
                else
                {
                    _logger.LogError("Cannot afford emergency machine - business operations halted!");
                }
            }
            return result;
        }

        // Process material reorders
        if (status.Sand.NeedsReorder)
        {
            var sandOrder = await CreateMaterialReorderAsync("sand", config.Sand.OrderQuantity);
            if (sandOrder != null)
            {
                result.SandOrderCreated = true;
                result.SandOrderId = sandOrder.Id;
                _logger.LogInformation("Sand order created: {OrderId}", sandOrder.Id);
            }
        }

        if (status.Copper.NeedsReorder)
        {
            var copperOrder = await CreateMaterialReorderAsync("copper", config.Copper.OrderQuantity);
            if (copperOrder != null)
            {
                result.CopperOrderCreated = true;
                result.CopperOrderId = copperOrder.Id;
                _logger.LogInformation("Copper order created: {OrderId}", copperOrder.Id);
            }
        }

        // Check equipment reorder with smart capital management

        if (await ShouldOrderNewMachine(workingMachines))
        {
            var equipmentOrder = await CreateEquipmentReorderAsync(config.Equipment.OrderQuantity);
            if (equipmentOrder != null)
            {
                result.EquipmentOrderCreated = true;
                result.EquipmentOrderId = equipmentOrder.Id;
                _logger.LogInformation("Equipment order created: {OrderId} - passed all material and financial checks", equipmentOrder.Id);
            }
        }
        else
        {
            _logger.LogInformation("Skipping equipment order - insufficient materials or capital for sustainable operation");
        }


        return result;
    }

    private async Task<PurchaseOrder?> CreateMaterialReorderAsync(string materialName, int quantity)
    {
        try
        {
            // Get pricing from Hand service
            var (bestPrice, bestSupplier, bestBankAccount, bestOrderId) = await GetBestMaterialPriceAsync(materialName, quantity);

            if (bestOrderId == -1)
            {
                _logger.LogWarning("No suitable {MaterialName} supplier found or insufficient funds", materialName);
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
            _logger.LogError(ex, "Failed to create {MaterialName} reorder", materialName);
            return null;
        }
    }

    private async Task<(decimal price, string supplier, string bankAccount, int orderId)> GetBestMaterialPriceAsync(string materialName, int quantity)
    {
        List<RawMaterialForSale> handMaterials = null;
        List<RecyclerMaterial> recyclerMaterials = null;
        RecyclerMaterial recyclerMaterial = null;
        RawMaterialForSale handMaterial = null;
        try
        {
            // Get pricing from Hand service
            try
            {
                handMaterials = await _handService.GetRawMaterialsForSaleAsync();
                handMaterial = handMaterials.FirstOrDefault(m =>
                m.RawMaterialName.Equals(materialName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex) { }
            ;
            try
            {
                recyclerMaterials = await _recyclerService.GetMaterialsAsync();
                recyclerMaterial = recyclerMaterials.FirstOrDefault(m =>
                m.Name.Equals(materialName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex) { }
            ;

            var bothAvailable = handMaterial != null && recyclerMaterial != null;


            if (handMaterial != null && (bothAvailable ? handMaterial.PricePerKg <= (decimal)recyclerMaterial.Price : true))
            {
                var totalCost = (int)(handMaterial.PricePerKg * quantity);

                // Check if we have sufficient balance including safety margin
                if (await _bankService.HasSufficientBalanceAsync(totalCost))
                {
                    // Make purchase request to get order details
                    var purchaseRequest = new PurchaseRawMaterialRequest
                    {
                        MaterialName = handMaterial.RawMaterialName,
                        WeightQuantity = quantity
                    };

                    var purchaseResponse = await _handService.PurchaseRawMaterialAsync(purchaseRequest);

                    return ((int)Math.Ceiling(purchaseResponse.Price / quantity), "thoh", purchaseResponse.BankAccount, purchaseResponse.OrderId);
                }
            }

            if (recyclerMaterial != null)
            {
                var totalCost = (int)(recyclerMaterial.Price * quantity);

                // Check if we have sufficient balance including safety margin
                if (await _bankService.HasSufficientBalanceAsync(totalCost))
                {
                    // Make purchase request to get order details
                    var purchaseRequest = new RecyclerOrderRequest
                    {
                        CompanyName = "screen-supplier",
                        Items = new List<RecyclerOrderItem>
                        {
                            new RecyclerOrderItem
                            {
                                RawMaterialName = materialName,
                                QuantityInKg = quantity
                            }
                        }
                    };

                    var purchaseResponse = await _recyclerService.CreateOrderAsync(purchaseRequest);

                    return ((purchaseResponse.data.OrderItems[0].pricePerKg), "recycler", purchaseResponse.data.AccountNumber, purchaseResponse.data.OrderId);
                }
            }

            throw new ExternalServiceException("Hand + Recycler is down.", "Its down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {MaterialName} pricing", materialName);
        }

        return (0, "", "", -1);
    }

    private async Task<PurchaseOrder?> CreateEquipmentReorderAsync(int quantity)
    {
        try
        {
            // Get equipment pricing from Hand service
            var (equipmentPrice, bankAccount, orderId) = await GetEquipmentPriceAsync(quantity);

            if (orderId == -1)
            {
                _logger.LogWarning("No suitable equipment supplier found or insufficient funds");
                return null;
            }

            var purchaseOrder = await _purchaseOrderService.CreatePurchaseOrderAsync(
                orderId,
                quantity,
                (int)equipmentPrice,
                bankAccount,
                "thoh",
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
            _logger.LogError(ex, "Failed to create equipment reorder");
            return null;
        }
    }

    private async Task<(decimal price, string bankAccount, int orderId)> GetEquipmentPriceAsync(int quantity)
    {
        try
        {
            // Get machine details from Hand
            var machinesResponse = await _handService.GetMachinesForSaleAsync();
            var screenMachine = machinesResponse.Machines.FirstOrDefault(m => m.MachineName == "screen_machine");

            if (screenMachine == null || screenMachine.Quantity < quantity)
            {
                return (0, "", -1);
            }

            var totalCost = screenMachine.Price * quantity;

            // Check if we have sufficient balance including safety margin
            if (await _bankService.HasSufficientBalanceAsync(totalCost))
            {
                // Make purchase request to get pricing and order details
                var purchaseRequest = new PurchaseMachineRequest
                {
                    MachineName = "screen_machine",
                    Quantity = quantity
                };

                var purchaseResponse = await _handService.PurchaseMachineAsync(purchaseRequest);

                return ((int)Math.Ceiling(purchaseResponse.Price / quantity), purchaseResponse.BankAccount, purchaseResponse.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment pricing");
        }

        return (0, "", -1);
    }


    private async Task<bool> ShouldOrderNewMachine(int currentMachines)
    {
        try
        {
            var equipmentParams = await _equipmentService.GetEquipmentParametersAsync();
            if (equipmentParams == null) return false;

            // Get current material levels
            var sandMaterial = await _materialService.GetMaterialAsync("sand");
            var copperMaterial = await _materialService.GetMaterialAsync("copper");

            var currentSand = sandMaterial?.Quantity ?? 0;
            var currentCopper = copperMaterial?.Quantity ?? 0;

            // Calculate total daily consumption for current machines + 1 new machine
            var totalMachines = currentMachines + 1;
            var dailySandNeeded = equipmentParams.InputSandKg * totalMachines;
            var dailyCopperNeeded = equipmentParams.InputCopperKg * totalMachines;

            // Configuration: How many days of materials we want as buffer
            var requiredDaysBuffer = 14;

            // Calculate required stock levels for the buffer period
            var requiredSandStock = dailySandNeeded * requiredDaysBuffer;
            var requiredCopperStock = dailyCopperNeeded * requiredDaysBuffer;

            // Check if we have enough materials for the buffer period
            if (currentSand < requiredSandStock || currentCopper < requiredCopperStock)
            {
                _logger.LogInformation(
                    "Insufficient materials for new machine. Need {RequiredDays} days buffer. " +
                    "Sand: {CurrentSand}/{RequiredSand}, Copper: {CurrentCopper}/{RequiredCopper}",
                    requiredDaysBuffer, currentSand, requiredSandStock, currentCopper, requiredCopperStock);
                return false;
            }

            // Additional check: Ensure we can afford the machine + replenishment materials
            var machinesResponse = await _handService.GetMachinesForSaleAsync();
            var screenMachine = machinesResponse.Machines.FirstOrDefault(m => m.MachineName == "screen_machine");
            if (screenMachine == null) return false;

            // Calculate cost to replenish materials back to target after machine purchase
            var config = _targetConfig.CurrentValue;
            var sandToReplenish = Math.Max(0, config.Sand.Target - (currentSand - requiredSandStock));
            var copperToReplenish = Math.Max(0, config.Copper.Target - (currentCopper - requiredCopperStock));

            var sandCostPerKg = await _materialService.GetAverageCostPerKgAsync("sand");
            var copperCostPerKg = await _materialService.GetAverageCostPerKgAsync("copper");

            var replenishmentCost = (int)((sandToReplenish * sandCostPerKg) + (copperToReplenish * copperCostPerKg));
            var totalCost = screenMachine.Price + replenishmentCost;

            var canAfford = await _bankService.HasSufficientBalanceAsync(totalCost);

            if (!canAfford)
            {
                _logger.LogInformation(
                    "Cannot afford new machine + material replenishment. Cost: {TotalCost} " +
                    "(Machine: {MachineCost}, Replenishment: {ReplenishmentCost})",
                    totalCost, screenMachine.Price, replenishmentCost);
            }

            return canAfford;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating new machine requirements");
            return false;
        }
    }

    private async Task<bool> CanAffordEmergencyMachine()
    {
        try
        {
            // Get machine cost
            var machinesResponse = await _handService.GetMachinesForSaleAsync();
            var screenMachine = machinesResponse.Machines.FirstOrDefault(m => m.MachineName == "screen_machine");
            if (screenMachine == null) return false;

            // For emergency situations, we only check if we can afford the machine
            // (not materials, since we can't produce anyway without a machine)
            return await _bankService.HasSufficientBalanceAsync(screenMachine.Price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking emergency machine affordability");
            return false;
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
}