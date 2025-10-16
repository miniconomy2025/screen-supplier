using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.Services;

public interface IProductionHistoryService
{
    Task<ProductionHistory?> GetProductionHistoryByDateAsync(DateTime date);
    Task<ProductionHistory> StoreDailyProductionHistory(int? screensProduced, DateTime? inputDate);
}