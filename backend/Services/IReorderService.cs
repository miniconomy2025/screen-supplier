namespace ScreenProducerAPI.Services;

public interface IReorderService
{
    Task<ReorderService.ReorderResult> CheckAndProcessReordersAsync();
}