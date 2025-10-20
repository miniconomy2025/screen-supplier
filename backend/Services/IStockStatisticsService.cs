using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.Services
{
    public interface IStockStatisticsService
    {
        Task<AllMaterialStatistics> GetMaterialStatisticsAsync();
    }
}