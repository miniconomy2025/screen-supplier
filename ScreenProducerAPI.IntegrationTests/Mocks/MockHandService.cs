using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;

namespace ScreenProducerAPI.IntegrationTests.Mocks;

public class MockHandService : IHandService
{
    private static int _orderIdCounter = 1;

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
                },
                new MachineForSale
                {
                    MachineName = "screen_machine",
                    Quantity = 5,
                    Price = 8500,
                    ProductionRate = 200,
                    Weight = 2000,
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
        // Simulate error conditions for specific test scenarios
        if (request.MachineName == "INSUFFICIENT_STOCK_MACHINE")
        {
            throw new InsufficientStockException("machines", request.Quantity, 0);
        }

        if (request.MachineName == "NETWORK_ERROR_MACHINE")
        {
            throw new HandServiceException("Hand service unavailable for machine purchase", 
                new HttpRequestException("Network error"));
        }

        if (request.MachineName == "TIMEOUT_MACHINE")
        {
            throw new HandServiceException("Hand service timeout during machine purchase", 
                new TaskCanceledException("Timeout"));
        }

        var response = new PurchaseMachineResponse
        {
            OrderId = System.Threading.Interlocked.Increment(ref _orderIdCounter),
            MachineName = request.MachineName,
            Quantity = request.Quantity,
            TotalPrice = 10000 * request.Quantity,
            UnitWeight = 500,
            BankAccount = "MOCK-HAND-ACC"
        };

        return Task.FromResult(response);
    }

    public Task<PurchaseRawMaterialResponse> PurchaseRawMaterialAsync(PurchaseRawMaterialRequest request)
    {
        // Simulate error conditions for specific test scenarios
        if (request.MaterialName == "INSUFFICIENT_STOCK_MATERIAL")
        {
            throw new InsufficientStockException(request.MaterialName, (int)request.WeightQuantity, 0);
        }

        if (request.MaterialName == "NETWORK_ERROR_MATERIAL")
        {
            throw new HandServiceException("Hand service unavailable for raw material purchase", 
                new HttpRequestException("Network error"));
        }

        if (request.MaterialName == "TIMEOUT_MATERIAL")
        {
            throw new HandServiceException("Hand service timeout during raw material purchase", 
                new TaskCanceledException("Timeout"));
        }

        var pricePerKg = request.MaterialName.ToLower() == "sand" ? 10m : 50m;
        
        var response = new PurchaseRawMaterialResponse
        {
            OrderId = System.Threading.Interlocked.Increment(ref _orderIdCounter),
            MaterialName = request.MaterialName,
            WeightQuantity = request.WeightQuantity,
            Price = pricePerKg * request.WeightQuantity,
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
            isOnline = true,
            IsRunning = false,
            EpochStartTime = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds()
        };

        return Task.FromResult<HandSimulationStatus?>(status);
    }

    public Task<bool> TryInitializeEquipmentParametersAsync(IEquipmentService equipmentService)
    {
        // Return false if equipmentService is null to simulate failure
        if (equipmentService == null)
            return Task.FromResult(false);

        return Task.FromResult(true);
    }
}
