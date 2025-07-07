using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;

namespace ScreenProducerAPI.Services;

public class ProductionHistoryService(ScreenContext context, ILogger<ProductionHistoryService> logger, MaterialService materialService, ProductService productService, SimulationTimeProvider simulationTimeProvider)
{
    public async Task<ProductionHistory?> GetProductionHistoryByDateAsync(DateTime date)
    {
        ProductionHistory productionHistory = null;

        foreach (var item in context.ProductionHistory)
        {
            if (item.RecordDate == date)
            {
                productionHistory = item;
                break;
            }
        }

        if (productionHistory == null)
        {
            logger.LogWarning("No production history found in the database, querying and saving now.");
            return null;
        }

        return productionHistory;
    }

    public async Task<ProductionHistory> StoreDailyProductionHistory(int? screensProduced)
    {
        try
        {
            screensProduced ??= 0;

            ProductionHistory existingRecord = null;

            foreach (var item in context.ProductionHistory)
            {
                if (item.RecordDate == simulationTimeProvider.Now)
                {
                    existingRecord = item;
                    break;
                }
            }

            var materials = await materialService.GetAllMaterialsAsync();

            var products = await productService.GetProductsAsync();

            var productionHistory = new ProductionHistory
            {
                RecordDate = simulationTimeProvider.Now,
                SandStock = materials.Where(materials => materials.Name.ToLower() == "sand").Sum(material => material.Quantity),
                CopperStock = materials.Where(materials => materials.Name.ToLower() == "copper").Sum(material => material.Quantity),
                ScreensProduced = screensProduced ?? 0
            };

            if (existingRecord == null)
            {
                logger.LogInformation("Saving production history");
                context.ProductionHistory.Add(productionHistory);
            }
            else
            {
                logger.LogInformation("Production history for today already exists. Overwriting.");
                existingRecord.SandStock = productionHistory.SandStock;
                existingRecord.CopperStock = productionHistory.CopperStock;
                existingRecord.ScreensProduced = productionHistory.ScreensProduced;
            }
            await context.SaveChangesAsync();

            return existingRecord ?? productionHistory;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error storing daily production history");
            throw;
        }
    }
}
