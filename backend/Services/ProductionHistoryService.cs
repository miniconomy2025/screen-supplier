using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;

namespace ScreenProducerAPI.Services;

public class ProductionHistoryService(ScreenContext context, ILogger<ProductionHistoryService> logger, MaterialService materialService, ProductService productService, EquipmentService equipmentService, SimulationTimeProvider simulationTimeProvider)
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
            return null;
        }

        return productionHistory;
    }

    public async Task<ProductionHistory> StoreDailyProductionHistory(int? screensProduced, DateTime? inputDate)
    {
        var date = inputDate ?? simulationTimeProvider.Now.Date;

        try
        {
            if (screensProduced == null || screensProduced == 0)
            {
                var activeEquipment = await equipmentService.GetActiveEquipmentAsync();
                screensProduced = activeEquipment.Sum(e => e.EquipmentParameters?.OutputScreens ?? 0);
            }

            ProductionHistory existingRecord = null;

            foreach (var item in context.ProductionHistory)
            {
                if (item.RecordDate == date)
                {
                    existingRecord = item;
                    break;
                }
            }

            var materials = await materialService.GetAllMaterialsAsync();

            var products = await productService.GetProductsAsync();

            var equipment = await context.Equipment
                .Where(e => e.IsAvailable || e.IsProducing)
                .ToListAsync();

            var productionHistory = new ProductionHistory
            {
                RecordDate = date,
                SandStock = materials.Where(materials => materials.Name.ToLower() == "sand").Sum(material => material.Quantity),
                CopperStock = materials.Where(materials => materials.Name.ToLower() == "copper").Sum(material => material.Quantity),
                ScreensProduced = screensProduced ?? 0,
                ScreenStock = products.First()?.Quantity ?? 0,
                ScreenPrice = products.First()?.Price ?? 0,
                WorkingEquipment = equipment.Count
            };

            if (existingRecord == null)
            {
                context.ProductionHistory.Add(productionHistory);
            }
            else
            {
                existingRecord.SandStock = productionHistory.SandStock;
                existingRecord.CopperStock = productionHistory.CopperStock;
                existingRecord.ScreensProduced = productionHistory.ScreensProduced;
                existingRecord.ScreenStock = productionHistory.ScreenStock;
                existingRecord.ScreenPrice = productionHistory.ScreenPrice;
                existingRecord.WorkingEquipment = productionHistory.WorkingEquipment;
            }
            await context.SaveChangesAsync();


            return existingRecord ?? productionHistory;
        }
        catch (Exception ex)
        {
            throw new Exception("Error saving production history");
        }
    }
}
