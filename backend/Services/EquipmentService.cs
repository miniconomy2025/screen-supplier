using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Models.Responses;

public class EquipmentService
{
    private readonly ScreenContext _context;
    private readonly MaterialService _materialService;
    private readonly ProductService _productService;

    public EquipmentService(ScreenContext context,
        MaterialService materialService, ProductService productService)
    {
        _context = context;
        _materialService = materialService;
        _productService = productService;
    }

    public async Task<bool> AddEquipmentAsync(int purchaseOrderId)
    {
        try
        {
            var equipmentParams = await _context.EquipmentParameters.FirstOrDefaultAsync();

            if (equipmentParams == null)
            {
                return false;
            }

            var equipment = new Equipment
            {
                ParametersID = equipmentParams.Id,
                IsProducing = false,
                IsAvailable = true,
                PurchaseOrderId = purchaseOrderId
            };

            _context.Equipment.Add(equipment);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<int> StartProductionAsync()
    {
        int machinesStarted = 0;

        try
        {
            var availableEquipment = await _context.Equipment
                .Include(e => e.EquipmentParameters)
                .Where(e => e.IsAvailable && !e.IsProducing)
                .ToListAsync();

            foreach (var equipment in availableEquipment)
            {
                if (equipment.EquipmentParameters == null)
                {
                    continue;
                }

                var prdouctionParams = equipment.EquipmentParameters;

                // Check if we have sufficient materials for this machine
                var hasSand = await _materialService.HasSufficientMaterialsAsync("sand", prdouctionParams.InputSandKg);
                var hasCopper = await _materialService.HasSufficientMaterialsAsync("copper", prdouctionParams.InputCopperKg);

                if (hasSand && hasCopper)
                {
                    // Consume the materials
                    var sandConsumed = await _materialService.ConsumeMaterialAsync("sand", prdouctionParams.InputSandKg);
                    var copperConsumed = await _materialService.ConsumeMaterialAsync("copper", prdouctionParams.InputCopperKg);

                    if (sandConsumed && copperConsumed)
                    {
                        equipment.IsProducing = true;
                        machinesStarted++;
                    }
                    else
                    {
                        break; // Stop trying if material consumption fails
                    }
                }
                else
                {
                    break; // Stop when we run out of materials
                }
            }

            if (machinesStarted > 0)
            {
                await _context.SaveChangesAsync();
            }

            return machinesStarted;
        }
        catch (Exception ex)
        {
            return machinesStarted;
        }
    }

    public async Task<int> StopProductionAsync()
    {
        int totalScreensProduced = 0;

        try
        {
            var producingEquipment = await _context.Equipment
                .Include(e => e.EquipmentParameters)
                .Where(e => e.IsProducing)
                .ToListAsync();

            foreach (var equipment in producingEquipment)
            {
                if (equipment.EquipmentParameters == null)
                {
                    continue;
                }

                var screensProduced = equipment.EquipmentParameters.OutputScreens;
                totalScreensProduced += screensProduced;

                equipment.IsProducing = false;
            }

            if (totalScreensProduced > 0)
            {
                // Add produced screens to inventory
                await _productService.AddScreensAsync(totalScreensProduced);

                // Update unit price based on current material costs
                await _productService.UpdateUnitPriceAsync();

                await _context.SaveChangesAsync();
            }

            return totalScreensProduced;
        }
        catch (Exception ex)
        {
            return totalScreensProduced;
        }
    }

    public async Task<List<Equipment>> GetAvailableEquipmentAsync()
    {
        return await _context.Equipment
            .Include(e => e.EquipmentParameters)
            .Where(e => e.IsAvailable && !e.IsProducing)
            .ToListAsync();
    }

    public async Task<List<Equipment>> GetActiveEquipmentAsync()
    {
        return await _context.Equipment
            .Include(e => e.EquipmentParameters)
            .Where(e => e.IsProducing)
            .ToListAsync();
    }

    public async Task<int> GetTotalDailyCapacityAsync()
    {
        var equipment = await _context.Equipment
            .Include(e => e.EquipmentParameters)
            .Where(e => e.IsAvailable)
            .ToListAsync();

        return equipment.Sum(e => e.EquipmentParameters?.OutputScreens ?? 0);
    }

    public async Task<bool> InitializeEquipmentParametersAsync(int inputSandKg, int inputCopperKg, int outputScreensPerDay, int machineWeight)
    {
        try
        {
            var existingParams = await _context.EquipmentParameters.FirstOrDefaultAsync();
            if (existingParams != null)
            {
                return true;
            }

            var equipmentParams = new EquipmentParameters
            {
                InputSandKg = inputSandKg,
                InputCopperKg = inputCopperKg,
                OutputScreens = outputScreensPerDay,
                EquipmentWeight = machineWeight
            };

            _context.EquipmentParameters.Add(equipmentParams);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<EquipmentParameters?> GetEquipmentParametersAsync()
    {
        return await _context.EquipmentParameters.FirstOrDefaultAsync();
    }

    public async Task<List<Equipment>> GetAllEquipmentAsync()
    {
        return await _context.Equipment
            .Include(e => e.EquipmentParameters)
            .Include(e => e.PurchaseOrder)
            .ToListAsync();
    }
    
    public async Task<MachineFailureResponse> ProcessMachineFailureAsync(int failureQty)
    {
        var availableMachines = await _context.Equipment
            .Where(e => e.IsAvailable)
            .Take(failureQty)
            .ToListAsync();

        if (!availableMachines.Any())
        {
            return new MachineFailureResponse
            {
                Success = false,
                FailedCount = 0,
                Message = "No available machines found to mark as failed."
            };
        }

        foreach (var machine in availableMachines)
        {
            machine.IsAvailable = false;
            machine.IsProducing = false;
        }

        await _context.SaveChangesAsync();

        return new MachineFailureResponse
        {
            Success = true,
            FailedCount = availableMachines.Count,
            Message = $"{availableMachines.Count} machines have experienced failure."
        };
    }
}