using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;

namespace ScreenProducerAPI.Services;

public class ProductService
{
    private readonly ScreenContext _context;
    private readonly ILogger<ProductService> _logger;
    private readonly MaterialService _materialService;

    public ProductService(ScreenContext context, ILogger<ProductService> logger, MaterialService materialService)
    {
        _context = context;
        _logger = logger;
        _materialService = materialService;
    }

    public async Task<bool> AddScreensAsync(int quantity)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync();

            if (product == null)
            {
                // Create initial product entry
                product = new Product
                {
                    Quantity = quantity,
                    Price = 0 // Will be calculated by UpdateUnitPrice
                };
                _context.Products.Add(product);
                _logger.LogInformation("Created initial product entry with {Quantity} screens", quantity);
            }
            else
            {
                product.Quantity += quantity;
                _logger.LogInformation("Added {Quantity} screens to inventory. New total: {NewTotal}",
                    quantity, product.Quantity);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding {Quantity} screens to inventory", quantity);
            return false;
        }
    }

    public async Task<bool> ConsumeScreensAsync(int quantity)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync();

            if (product == null || product.Quantity < quantity)
            {
                _logger.LogWarning("Insufficient screens: requested {Requested}, available {Available}",
                    quantity, product?.Quantity ?? 0);
                return false;
            }

            product.Quantity -= quantity;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Consumed {Quantity} screens. Remaining: {Remaining}",
                quantity, product.Quantity);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming {Quantity} screens", quantity);
            return false;
        }
    }

    public async Task<bool> UpdateUnitPriceAsync()
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync();
            if (product == null)
            {
                _logger.LogWarning("No product found to update unit price");
                return false;
            }

            // Get equipment parameters to understand material ratios
            var equipmentParams = await _context.EquipmentParameters.FirstOrDefaultAsync();
            if (equipmentParams == null)
            {
                _logger.LogError("No equipment parameters found for price calculation");
                return false;
            }

            // Get average material costs
            var sandCostPerKg = await _materialService.GetAverageCostPerKgAsync("sand");
            var copperCostPerKg = await _materialService.GetAverageCostPerKgAsync("copper");

            // Calculate cost per screen based on material requirements
            var sandCostPerScreen = (sandCostPerKg * equipmentParams.InputSandKg) / equipmentParams.OutputScreens;
            var copperCostPerScreen = (copperCostPerKg * equipmentParams.InputCopperKg) / equipmentParams.OutputScreens;

            var materialCostPerScreen = sandCostPerScreen + copperCostPerScreen;

            // Add margin (e.g., 25% markup)
            var margin = 0.25m;
            var unitPrice = materialCostPerScreen * (1 + margin);

            product.Price = (int)Math.Ceiling(unitPrice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated unit price to {UnitPrice} (Sand: {SandCost}, Copper: {CopperCost}, Margin: {Margin}%)",
                product.Price, sandCostPerScreen, copperCostPerScreen, margin * 100);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating unit price");
            return false;
        }
    }

    public async Task<Product?> GetProductAsync()
    {
        return await _context.Products.FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        return await _context.Products.ToListAsync();
    }
}
