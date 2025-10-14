using ScreenProducerAPI.Services.SupplierService.Hand.Models;

namespace ScreenProducerAPI.Services;

public interface IHandService
{
    Task<MachinesForSaleResponse> GetMachinesForSaleAsync();
    Task<List<RawMaterialForSale>> GetRawMaterialsForSaleAsync();
    Task<PurchaseMachineResponse> PurchaseMachineAsync(PurchaseMachineRequest request);
    Task<PurchaseRawMaterialResponse> PurchaseRawMaterialAsync(PurchaseRawMaterialRequest request);
    Task<SimulationTimeResponse> GetCurrentSimulationTimeAsync();
    Task<HandSimulationStatus?> GetSimulationStatusAsync();
    Task<bool> TryInitializeEquipmentParametersAsync(IEquipmentService equipmentService);
}