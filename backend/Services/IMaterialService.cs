using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.Services;

public interface IMaterialService
{
    Task<bool> AddMaterialAsync(string materialName, int quantity);
    Task<bool> ConsumeMaterialAsync(string materialName, int quantity);
    Task<bool> HasSufficientMaterialsAsync(string materialName, int requiredQuantity);
    Task<Material?> GetMaterialAsync(string materialName);
    Task<List<Material>> GetAllMaterialsAsync();
    Task<decimal> GetAverageCostPerKgAsync(string materialName);
}