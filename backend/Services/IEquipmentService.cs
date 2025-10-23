using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;

namespace ScreenProducerAPI.Services;

public interface IEquipmentService
{
    Task<bool> AddEquipmentAsync(int purchaseOrderId);
    Task<List<Equipment>> GetActiveEquipmentAsync();
    Task<List<Equipment>> GetAvailableEquipmentAsync();
    Task<List<Equipment>> GetAllEquipmentAsync();
    Task<int> StartProductionAsync();
    Task<int> StopProductionAsync();
    Task<int> GetTotalDailyCapacityAsync();
    Task<bool> InitializeEquipmentParametersAsync(int sandKg, int copperKg, int outputScreens, int equipmentWeight);
    Task<EquipmentParameters?> GetEquipmentParametersAsync();
    Task<MachineFailureResponse> ProcessMachineFailureAsync(int failureQty);
}