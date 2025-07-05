using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.ScreenDbContext;

namespace ScreenProducerAPI.Services;

public class StockStatisticsService(
    ScreenContext context,
    IOptionsMonitor<TargetQuantitiesConfig> targetConfig,
    IOptionsMonitor<StockManagementOptions> stockConfig)
{
    public async Task<AllMaterialStatistics> GetMaterialStatisticsAsync()
    {
        var equipment = await context.Equipment.Where(equipment => equipment.IsAvailable || equipment.IsProducing).ToListAsync();

        var machineCount = equipment.Count();

        if (machineCount == 0)
        {
            return new AllMaterialStatistics();
        }

        var equipmentParameters = equipment.First().EquipmentParameters;


        return new AllMaterialStatistics
        {
            Sand = new MaterialStatistics()
            {
                DailyConsumption = equipmentParameters.InputSandKg * machineCount,
                ReorderPoint = (equipmentParameters.InputSandKg * machineCount * stockConfig.CurrentValue.LogisticsLeadTimeDays) + targetConfig.CurrentValue.Sand.Target,
            },
            Copper = new MaterialStatistics()
            {
                DailyConsumption = equipmentParameters.InputCopperKg * machineCount,
                ReorderPoint = (equipmentParameters.InputCopperKg * machineCount * stockConfig.CurrentValue.LogisticsLeadTimeDays) + targetConfig.CurrentValue.Sand.Target,
            }
        };
    }
}

public class StockManagementOptions : IOptions<StockManagementOptions>
{
    public int MaxScreens { get; set; }
    public int LogisticsLeadTimeDays { get; set; }
    public static string Section { get; } = "StockManagement";
    public StockManagementOptions Value => this;
}

