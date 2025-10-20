using ScreenProducerAPI.Models;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;

namespace ScreenProducerAPI.Services;

public interface IEquipmentService
{
    Task<List<Equipment>> GetActiveEquipmentAsync();
    Task<List<Equipment>> GetAllEquipmentAsync();
    Task<int> StartProductionAsync();
    Task<int> StopProductionAsync();
    Task<bool> InitializeEquipmentParametersAsync(int sandKg, int copperKg, int outputScreens, int equipmentWeight);
    Task<EquipmentParameters?> GetEquipmentParametersAsync();
}