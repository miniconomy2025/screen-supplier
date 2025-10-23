using ScreenProducerAPI.Models;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Services
{
    public interface IStockStatisticsService
    {
        Task<AllMaterialStatistics> GetMaterialStatisticsAsync();
    }
}
