using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;

public class EquipmentService
{
    private readonly ScreenContext _context;
    private readonly ILogger<EquipmentService> _logger;
    private readonly MaterialService _materialService;
    private readonly ProductService _productService;

    public EquipmentService(ScreenContext context, ILogger<EquipmentService> logger, 
        MaterialService materialService, ProductService productService)
    {
        _context = context;
        _logger = logger;
        _materialService = materialService;
        _productService = productService;
    }

    public async Task<bool> AddEquipmentAsync(int purchaseOrderId)
    {
        try
        {
            // Get the equipment parameters (should be set during simulation start)
            var equipmentParams = await _context.EquipmentParameters.FirstOrDefaultAsync();
            
            if (equipmentParams == null)
            {
                _logger.LogError("No equipment parameters found. Equipment parameters must be initialized on simulation start.");
                return false;
            }

            // Create new equipment instance - only store the foreign key reference
            var equipment = new Equipment
            {
                ParametersID = equipmentParams.Id,
                IsProducing = false,
                IsAvailable = true,
                PurchaseOrderId = purchaseOrderId
            };

            _context.Equipment.Add(equipment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added new equipment with ID {EquipmentId} for purchase order {PurchaseOrderId}. " +
                "References parameters ID {ParametersId} with daily capacity: {OutputScreens} screens, requires {InputSand}kg sand + {InputCopper}kg copper",
                equipment.Id, purchaseOrderId, equipmentParams.Id, equipmentParams.OutputScreens, equipmentParams.InputSandKg, equipmentParams.InputCopperKg);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding equipment for purchase order {PurchaseOrderId}", purchaseOrderId);
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

            _logger.LogInformation("Attempting to start production on {AvailableCount} available machines", availableEquipment.Count);

            foreach (var equipment in availableEquipment)
            {
                if (equipment.EquipmentParameters == null)
                {
                    _logger.LogWarning("Equipment {EquipmentId} has no parameters, skipping", equipment.Id);
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
                        _logger.LogInformation("Started production on equipment {EquipmentId}. Consumed {SandKg}kg sand + {CopperKg}kg copper",
                            equipment.Id, prdouctionParams.InputSandKg, prdouctionParams.InputCopperKg);
                    }
                    else
                    {
                        _logger.LogError("Failed to consume materials for equipment {EquipmentId}", equipment.Id);
                        break; // Stop trying if material consumption fails
                    }
                }
                else
                {
                    _logger.LogInformation("Insufficient materials for equipment {EquipmentId}. Sand: {HasSand}, Copper: {HasCopper}",
                        equipment.Id, hasSand, hasCopper);
                    break; // Stop when we run out of materials
                }
            }

            if (machinesStarted > 0)
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Production started on {MachinesStarted} machines", machinesStarted);
            return machinesStarted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during production startup");
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

            _logger.LogInformation("Stopping production on {ProducingCount} machines", producingEquipment.Count);

            foreach (var equipment in producingEquipment)
            {
                if (equipment.EquipmentParameters == null)
                {
                    _logger.LogWarning("Equipment {EquipmentId} has no parameters, skipping production", equipment.Id);
                    continue;
                }

                var screensProduced = equipment.EquipmentParameters.OutputScreens;
                totalScreensProduced += screensProduced;
                
                equipment.IsProducing = false;
                
                _logger.LogInformation("Equipment {EquipmentId} produced {ScreensProduced} screens",
                    equipment.Id, screensProduced);
            }

            if (totalScreensProduced > 0)
            {
                // Add produced screens to inventory
                await _productService.AddScreensAsync(totalScreensProduced);
                
                // Update unit price based on current material costs
                await _productService.UpdateUnitPriceAsync();
                
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Production stopped. Total screens produced: {TotalScreens}", totalScreensProduced);
            return totalScreensProduced;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during production shutdown");
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

    public async Task<bool> InitializeEquipmentParametersAsync(int inputSandKg, int inputCopperKg, int outputScreensPerDay)
    {
        try
        {
            // Check if parameters already exist
            var existingParams = await _context.EquipmentParameters.FirstOrDefaultAsync();
            if (existingParams != null)
            {
                _logger.LogInformation("Equipment parameters already exist, skipping initialization");
                return true;
            }

            var equipmentParams = new EquipmentParameters
            {
                InputSandKg = inputSandKg,
                InputCopperKg = inputCopperKg,
                OutputScreens = outputScreensPerDay
            };

            _context.EquipmentParameters.Add(equipmentParams);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Initialized equipment parameters: {InputSand}kg sand + {InputCopper}kg copper → {OutputScreens} screens/day",
                inputSandKg, inputCopperKg, outputScreensPerDay);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing equipment parameters");
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
}