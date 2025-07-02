using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Services;

public class TargetQuantityService
{
    private readonly ScreenContext _context;
    private readonly ILogger<TargetQuantityService> _logger;
    private readonly IOptionsMonitor<TargetQuantitiesConfig> _targetConfig;

    public TargetQuantityService(
        ScreenContext context,
        ILogger<TargetQuantityService> logger,
        IOptionsMonitor<TargetQuantitiesConfig> targetConfig)
    {
        _context = context;
        _logger = logger;
        _targetConfig = targetConfig;
    }

    public async Task<InventoryStatus> GetInventoryStatusAsync()
    {
        var config = _targetConfig.CurrentValue;

        // Get current material quantities
        var sandMaterial = await _context.Materials.FirstOrDefaultAsync(m => m.Name.ToLower() == "sand");
        var copperMaterial = await _context.Materials.FirstOrDefaultAsync(m => m.Name.ToLower() == "copper");

        var currentSand = sandMaterial?.Quantity ?? 0;
        var currentCopper = copperMaterial?.Quantity ?? 0;

        // Get current available equipment count
        var currentEquipment = await _context.Equipment.CountAsync(e => e.IsAvailable);

        // Get incoming quantities from active purchase orders
        var incomingSand = await GetIncomingMaterialQuantityAsync("sand");
        var incomingCopper = await GetIncomingMaterialQuantityAsync("copper");
        var incomingEquipment = await GetIncomingEquipmentQuantityAsync();

        return new InventoryStatus
        {
            Sand = new MaterialStatus
            {
                Current = currentSand,
                Incoming = incomingSand,
                Total = currentSand + incomingSand,
                Target = config.Sand.Target,
                ReorderPoint = config.Sand.ReorderPoint,
                NeedsReorder = (currentSand + incomingSand) <= config.Sand.ReorderPoint
            },
            Copper = new MaterialStatus
            {
                Current = currentCopper,
                Incoming = incomingCopper,
                Total = currentCopper + incomingCopper,
                Target = config.Copper.Target,
                ReorderPoint = config.Copper.ReorderPoint,
                NeedsReorder = (currentCopper + incomingCopper) <= config.Copper.ReorderPoint
            },
            Equipment = new EquipmentStatus
            {
                Current = currentEquipment,
                Incoming = incomingEquipment,
                Total = currentEquipment + incomingEquipment,
                Target = config.Equipment.Target,
                ReorderPoint = config.Equipment.ReorderPoint,
                NeedsReorder = (currentEquipment + incomingEquipment) <= config.Equipment.ReorderPoint
            }
        };
    }

    private async Task<int> GetIncomingMaterialQuantityAsync(string materialName)
    {
        var incomingStates = new[]
        {
            Status.RequiresPaymentToSupplier,
            Status.RequiresDelivery,
            Status.RequiresPaymentToLogistics,
            Status.WaitingForDelivery
        };

        var incoming = await _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .Include(po => po.RawMaterial)
            .Where(po => po.RawMaterial != null &&
                        po.RawMaterial.Name.ToLower() == materialName.ToLower() &&
                        incomingStates.Contains(po.OrderStatus.Status) &&
                        po.EquipmentOrder != true)
            .SumAsync(po => po.Quantity - po.QuantityDelivered);

        return incoming;
    }

    private async Task<int> GetIncomingEquipmentQuantityAsync()
    {
        var incomingStates = new[]
        {
            Status.RequiresPaymentToSupplier,
            Status.RequiresDelivery,
            Status.RequiresPaymentToLogistics,
            Status.WaitingForDelivery
        };

        var incoming = await _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .Where(po => po.EquipmentOrder == true &&
                        incomingStates.Contains(po.OrderStatus.Status))
            .SumAsync(po => po.Quantity - po.QuantityDelivered);

        return incoming;
    }
}

public class InventoryStatus
{
    public MaterialStatus Sand { get; set; } = new();
    public MaterialStatus Copper { get; set; } = new();
    public EquipmentStatus Equipment { get; set; } = new();
}

public class MaterialStatus
{
    public int Current { get; set; }
    public int Incoming { get; set; }
    public int Total { get; set; }
    public int Target { get; set; }
    public int ReorderPoint { get; set; }
    public bool NeedsReorder { get; set; }
}

public class EquipmentStatus
{
    public int Current { get; set; }
    public int Incoming { get; set; }
    public int Total { get; set; }
    public int Target { get; set; }
    public int ReorderPoint { get; set; }
    public bool NeedsReorder { get; set; }
}