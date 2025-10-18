using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;

namespace ScreenProducerAPI.IntegrationTests.Mocks;

/// <summary>
/// Mock implementation of IHandService for integration testing.
/// Returns predictable test data instead of making real HTTP calls.
/// </summary>
public class MockHandService : IHandService
{
    public Task<MachinesForSaleResponse> GetMachinesForSaleAsync()
    {
        var response = new MachinesForSaleResponse
        {
            Machines = new List<MachineForSale>
            {
                new MachineForSale
                {
                    MachineName = "Test Machine 1",
                    Quantity = 10,
                    Price = 10000,
                    ProductionRate = 100,
                    Weight = 500,
                    InputRatio = new inputRatio { Copper = 2, Sand = 3 }
                }
            }
        };

        return Task.FromResult(response);
    }

    public Task<List<RawMaterialForSale>> GetRawMaterialsForSaleAsync()
    {
        var materials = new List<RawMaterialForSale>
        {
            new RawMaterialForSale
            {
                RawMaterialName = "Sand",
                PricePerKg = 10,
                QuantityAvailable = 10000
            },
            new RawMaterialForSale
            {
                RawMaterialName = "Copper",
                PricePerKg = 50,
                QuantityAvailable = 5000
            }
        };

        return Task.FromResult(materials);
    }

    public Task<PurchaseMachineResponse> PurchaseMachineAsync(PurchaseMachineRequest request)
    {
        var response = new PurchaseMachineResponse
        {
            OrderId = 1,
            MachineName = "Test Machine",
            Quantity = 1,
            TotalPrice = 10000,
            UnitWeight = 500,
            BankAccount = "MOCK-HAND-ACC"
        };

        return Task.FromResult(response);
    }

    public Task<PurchaseRawMaterialResponse> PurchaseRawMaterialAsync(PurchaseRawMaterialRequest request)
    {
        var response = new PurchaseRawMaterialResponse
        {
            OrderId = 1,
            MaterialName = "Sand",
            WeightQuantity = 100,
            Price = 1000,
            BankAccount = "MOCK-HAND-ACC"
        };

        return Task.FromResult(response);
    }

    public Task<SimulationTimeResponse> GetCurrentSimulationTimeAsync()
    {
        var response = new SimulationTimeResponse
        {
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Time = DateTime.UtcNow.ToString("HH:mm:ss")
        };

        return Task.FromResult(response);
    }

    public Task<HandSimulationStatus?> GetSimulationStatusAsync()
    {
        var status = new HandSimulationStatus
        {
            IsRunning = false,
            EpochStartTime = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds()
        };

        return Task.FromResult<HandSimulationStatus?>(status);
    }

    public Task<bool> TryInitializeEquipmentParametersAsync(IEquipmentService equipmentService)
    {
        return Task.FromResult(true);
    }
}
