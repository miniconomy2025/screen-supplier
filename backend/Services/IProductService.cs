using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.Services;

public interface IProductService
{
    Task<Product?> GetProductAsync();
    Task<(int totalScreens, int reservedScreens, int availableScreens)> GetStockSummaryAsync();
    Task<int> GetAvailableStockAsync();
    Task UpdateUnitPriceAsync();
}