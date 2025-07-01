using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;

namespace ScreenProducerAPI.Services;

public class MaterialService
{
    private readonly ScreenContext _context;
    private readonly ILogger<MaterialService> _logger;

    public MaterialService(ScreenContext context, ILogger<MaterialService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> AddMaterialAsync(string materialName, int quantity)
    {
        try
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Name.ToLower() == materialName.ToLower());

            if (material == null)
            {
                // Create new material entry
                material = new Material
                {
                    Name = materialName,
                    Quantity = quantity
                };
                _context.Materials.Add(material);
                _logger.LogInformation("Created new material: {MaterialName} with quantity {Quantity}",
                    materialName, quantity);
            }
            else
            {
                // Update existing material
                material.Quantity += quantity;
                _logger.LogInformation("Updated {MaterialName} inventory: +{AddedQuantity}, new total: {NewTotal}",
                    materialName, quantity, material.Quantity);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding material {MaterialName}, quantity {Quantity}",
                materialName, quantity);
            return false;
        }
    }

    public async Task<bool> ConsumeMaterialAsync(string materialName, int quantity)
    {
        try
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Name.ToLower() == materialName.ToLower());

            if (material == null)
            {
                _logger.LogWarning("Attempted to consume material {MaterialName} that doesn't exist", materialName);
                return false;
            }

            if (material.Quantity < quantity)
            {
                _logger.LogWarning("Insufficient {MaterialName}: requested {Requested}, available {Available}",
                    materialName, quantity, material.Quantity);
                return false;
            }

            material.Quantity -= quantity;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Consumed {MaterialName}: -{ConsumedQuantity}, remaining: {Remaining}",
                materialName, quantity, material.Quantity);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming material {MaterialName}, quantity {Quantity}",
                materialName, quantity);
            return false;
        }
    }

    public async Task<bool> HasSufficientMaterialsAsync(string materialName, int requiredQuantity)
    {
        var material = await _context.Materials
            .FirstOrDefaultAsync(m => m.Name.ToLower() == materialName.ToLower());

        return material != null && material.Quantity >= requiredQuantity;
    }

    public async Task<Material?> GetMaterialAsync(string materialName)
    {
        return await _context.Materials
            .FirstOrDefaultAsync(m => m.Name.ToLower() == materialName.ToLower());
    }

    public async Task<List<Material>> GetAllMaterialsAsync()
    {
        return await _context.Materials.ToListAsync();
    }

    public async Task<decimal> GetAverageCostPerKgAsync(string materialName)
    {
        // This would need to track purchase costs over time
        // For now, return a default or calculate from recent purchase orders
        var recentPurchases = await _context.PurchaseOrders
            .Include(po => po.RawMaterial)
            .Where(po => po.RawMaterial != null && po.RawMaterial.Name.ToLower() == materialName.ToLower())
            .OrderByDescending(po => po.OrderDate)
            .Take(5) // Last 5 purchases
            .ToListAsync();

        if (!recentPurchases.Any())
        {
            _logger.LogWarning("No recent purchases found for {MaterialName}, using default cost", materialName);
            return 100; // Default cost
        }

        var averageCost = recentPurchases.Average(po => (decimal)po.UnitPrice);
        _logger.LogInformation("Calculated average cost for {MaterialName}: {AverageCost}", materialName, averageCost);
        return averageCost;
    }
}