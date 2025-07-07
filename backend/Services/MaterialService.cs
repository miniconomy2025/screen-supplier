using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;

namespace ScreenProducerAPI.Services;

public class MaterialService
{
    private readonly ScreenContext _context;

    public MaterialService(ScreenContext context)
    {
        _context = context;
    }

    public async Task<bool> AddMaterialAsync(string materialName, int quantity)
    {
        try
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Name.ToLower() == materialName.ToLower());

            if (material == null)
            {
                material = new Material
                {
                    Name = materialName,
                    Quantity = quantity
                };
                _context.Materials.Add(material);
            }
            else
            {
                material.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
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
                return false;
            }

            if (material.Quantity < quantity)
            {
                return false;
            }

            material.Quantity -= quantity;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
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
        var recentPurchases = await _context.PurchaseOrders
            .Include(po => po.RawMaterial)
            .Where(po => po.RawMaterial != null && po.RawMaterial.Name.ToLower() == materialName.ToLower())
            .OrderByDescending(po => po.OrderDate)
            .Take(5)
            .ToListAsync();

        if (!recentPurchases.Any())
        {
            return 100; 
        }

        var averageCost = recentPurchases.Average(po => (decimal)po.UnitPrice);
        return averageCost;
    }
}