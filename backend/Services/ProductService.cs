using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Services;

public class ProductService
{
    private readonly ScreenContext _context;
    private readonly MaterialService _materialService;

    public ProductService(ScreenContext context, MaterialService materialService)
    {
        _context = context;
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
            }
            else
            {
                product.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
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
                return false;
            }

            product.Quantity -= quantity;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<int> GetAvailableStockAsync()
    {
        try
        {
            // Get total produced screens
            var product = await _context.Products.FirstOrDefaultAsync();
            var totalProduced = product?.Quantity ?? 0;

            // Get sum of screens in orders that are waiting_payment or waiting_collection
            // These are considered "reserved" and unavailable for new orders
            var reservedScreens = await _context.ScreenOrders
                .Include(so => so.OrderStatus)
                .Where(so => so.OrderStatus.Status == Status.WaitingForPayment ||
                           so.OrderStatus.Status == Status.WaitingForCollection)
                .SumAsync(so => so.Quantity);

            var availableStock = totalProduced - reservedScreens;

            return Math.Max(0, availableStock);
        }
        catch (Exception ex)
        {
            return 0;
        }
    }

    public async Task<bool> UpdateUnitPriceAsync()
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync();
            if (product == null)
            {
                return false;
            }

            // Get equipment parameters to understand material ratios
            var equipmentParams = await _context.EquipmentParameters.FirstOrDefaultAsync();
            if (equipmentParams == null)
            {
                return false;
            }

            // Get average material costs
            var sandCostPerKg = await _materialService.GetAverageCostPerKgAsync("sand");
            var copperCostPerKg = await _materialService.GetAverageCostPerKgAsync("copper");

            var machinesCost = await _context.PurchaseOrders
                .Where(x => x.EquipmentOrder == true && x.OrderStatusId == 8)
                .SumAsync(x => x.UnitPrice * x.Quantity);

            var totalScreensPurchase = await _context.ScreenOrders.Where(x => (x.OrderStatusId == 3)).SumAsync(x => x.Quantity);
            var screens = await _context.Products.FirstOrDefaultAsync();
            var screensInStock = screens != null ? screens.Quantity : 0;

            // Calculate cost per screen based on material requirement
            var sandCostPerScreen = sandCostPerKg * equipmentParams.InputSandKg / equipmentParams.OutputScreens;
            var copperCostPerScreen = copperCostPerKg * equipmentParams.InputCopperKg / equipmentParams.OutputScreens;
            var machineCostPerScreen = (decimal)machinesCost / (screensInStock + totalScreensPurchase + 1);

            var materialCostPerScreen = sandCostPerScreen + copperCostPerScreen + machineCostPerScreen;

            // Add margin (e.g., 25% markup)
            var margin = 0.25m;
            var unitPrice = materialCostPerScreen * (1 + margin);

            product.Price = (int)Math.Ceiling(unitPrice);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
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

    public async Task<(int totalProduced, int reserved, int available)> GetStockSummaryAsync()
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync();
            var totalProduced = product?.Quantity ?? 0;

            var reservedScreens = await _context.ScreenOrders
                .Include(so => so.OrderStatus)
                .Where(so => so.OrderStatus.Status == Status.WaitingForPayment ||
                           so.OrderStatus.Status == Status.WaitingForCollection)
                .SumAsync(so => so.Quantity);

            var available = Math.Max(0, totalProduced - reservedScreens);

            return (totalProduced, reservedScreens, available);
        }
        catch (Exception ex)
        {
            return (0, 0, 0);
        }
    }
}