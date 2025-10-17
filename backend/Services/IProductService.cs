using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.Services;

public interface IProductService
{
    Task<Product?> GetProductAsync();
    Task<(int totalProduced, int reserved, int available)> GetStockSummaryAsync();
    Task<int> GetAvailableStockAsync();
    Task<bool> UpdateUnitPriceAsync();
}