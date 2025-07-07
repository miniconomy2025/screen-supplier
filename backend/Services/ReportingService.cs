using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.Services;

public class ReportingService(ILogger<ReportingService> logger,
    ProductionHistoryService productionHistoryService,
    PurchaseOrderService purchaseOrderService,
    EquipmentService equipmentService,
    ScreenOrderService screenOrderService)
{
    public async Task<DailyReport?> GetDailyReportAsync(DateTime date)
    {
        try
        {
            var productionHistory = await productionHistoryService.GetProductionHistoryByDateAsync(date);
            var purchaseOrders = await purchaseOrderService.GetActivePurchaseOrdersAsync();
            var equipment = (await equipmentService.GetAllEquipmentAsync()).Where(equipment => equipment.IsAvailable || equipment.IsProducing).ToList();
            var screenOrders = await screenOrderService.GetOrdersByDateAsync(date);

            if (productionHistory == null)
            {
                logger.LogWarning("No production history found for the specified date: {Date}", date);
                return null;
            }

            List<PurchaseOrder> purchaseOrdersOnDate = [];

            foreach (var item in purchaseOrders)
            {
                if (item.OrderDate.Date == date.Date)
                {
                    purchaseOrdersOnDate.Add(item);
                }
            }

            List<ScreenOrder> screenOrdersOnDate = [];

            foreach (var item in screenOrders)
            {
                if (item.OrderDate.Date == date.Date)
                {
                    screenOrdersOnDate.Add(item);
                }
            }

            var sandPurchased = purchaseOrdersOnDate
                .Where(order => order.RawMaterial != null &&
                                string.Equals(order.RawMaterial.Name, nameof(Materials.Sand), StringComparison.OrdinalIgnoreCase))
                .Sum(order => order.Quantity);

            var copperPurchased = purchaseOrdersOnDate
                .Where(order => order.RawMaterial != null &&
                                string.Equals(order.RawMaterial.Name, nameof(Materials.Copper), StringComparison.OrdinalIgnoreCase))
                .Sum(order => order.Quantity);

            var equipmentParams = equipment.FirstOrDefault()?.EquipmentParameters;

            var sandConsumed = equipmentParams != null && productionHistory.ScreensProduced != 0 ? equipmentParams.InputSandKg / (productionHistory.ScreensProduced / equipmentParams.OutputScreens) : 0;
            var copperConsumed = equipmentParams != null && productionHistory.ScreensProduced != 0 ? equipmentParams.InputCopperKg / (productionHistory.ScreensProduced / equipmentParams.OutputScreens) : 0;


            var dailyProductionSummary = new DailyReport()
            {
                Date = date,
                SandStock = productionHistory.SandStock,
                CopperStock = productionHistory.CopperStock,
                SandPurchased = sandPurchased,
                CopperPurchased = copperPurchased,
                SandConsumed = sandConsumed,
                CopperConsumed = copperConsumed,
                ScreensProduced = productionHistory.ScreensProduced,
                WorkingMachines = equipment.Count,
                ScreensSold = screenOrdersOnDate.Sum(order => order.Quantity),
                Revenue = screenOrdersOnDate.Sum(order => order.UnitPrice * order.Quantity),
            };

            return dailyProductionSummary;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving recent production history");
            return null;
        }
    }

    internal async Task<List<DailyReport>> GetLastPeriodReportsAsync(int pastDaysToInclude, DateTime date)
    {
        var reports = new List<DailyReport>();

        for (int i = 0; i < pastDaysToInclude; i++)
        {
            var day = date.Date.AddDays(-i);
            var report = await GetDailyReportAsync(day);

            if (report != null)
            {
                reports.Add(report);
            }
        }
        return reports.OrderBy(r => r.Date).ToList();
    }
}
