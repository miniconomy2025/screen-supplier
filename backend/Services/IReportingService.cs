using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.Services;

public interface IReportingService
{
    Task<DailyReport?> GetDailyReportAsync(DateTime date);
    Task<List<DailyReport>> GetLastPeriodReportsAsync(int pastDaysToInclude, DateTime date);
}