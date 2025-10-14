namespace ScreenProducerAPI.Services;

public interface IProductionHistoryService
{
    Task StoreDailyProductionHistory(int screensProduced, DateTime? recordDate);
}