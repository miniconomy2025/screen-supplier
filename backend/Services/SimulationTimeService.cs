using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Services.SupplierService.Hand;

namespace ScreenProducerAPI.Services
{
    public class SimulationTimeService : IDisposable
    {
        private readonly ILogger<SimulationTimeService> _logger;
        private readonly IServiceProvider _serviceProvider;

        private long _simulationStartUnixEpoch;
        private bool _simulationRunning;
        private bool _bankAccountCreated = false;
        private bool _bankLoanCreated = false;
        private bool _notificationUrlSet = false;
        private bool _equipmentParametersInitialized = false;
        private Timer? _dayTimer;

        // 2 minutes real time = 1 simulation day (120 seconds)
        private const int RealTimeToSimDayMs = 2 * 60 * 1000;

        public SimulationTimeService(ILogger<SimulationTimeService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<bool> StartSimulationAsync(long unixEpochStart)
        {
            if (_simulationRunning)
            {
                StopSimulation();
            }

            _simulationStartUnixEpoch = unixEpochStart;

            // Initialize all required services
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();
            var bankIntegrationService = scope.ServiceProvider.GetRequiredService<BankIntegrationService>();
            var handService = scope.ServiceProvider.GetRequiredService<HandService>();
            var equipmentService = scope.ServiceProvider.GetRequiredService<EquipmentService>();

            await CleanUpDatabase(context);

            // Initialize bank integration
            (_bankAccountCreated, _bankLoanCreated, _notificationUrlSet) =
                await bankIntegrationService.InitializeAsync(_bankAccountCreated, _bankLoanCreated, _notificationUrlSet);

            // Initialize equipment parameters from Hand service
            if (!_equipmentParametersInitialized)
            {
                _equipmentParametersInitialized = await InitializeEquipmentParametersFromHand(handService, equipmentService);

                if (!_equipmentParametersInitialized)
                {
                    _logger.LogError("Failed to initialize equipment parameters from Hand service. Cannot start simulation.");
                    return false;
                }
            }

            _simulationRunning = true;

            // Start the timer for daily processing - first tick after 2 minutes
            _dayTimer = new Timer(ProcessDayTransition, null, RealTimeToSimDayMs, RealTimeToSimDayMs);

            // Trigger initial day 0 start immediately
            _ = Task.Run(async () => await TriggerStartOfDay(0));

            return true;
        }

        private async Task<bool> InitializeEquipmentParametersFromHand(HandService handService, EquipmentService equipmentService)
        {
            try
            {
                _logger.LogInformation("Fetching equipment parameters from Hand service...");

                var machinesResponse = await handService.GetMachinesForSaleAsync();
                var screenMachine = machinesResponse.Machines.FirstOrDefault(m => m.MachineName == "screen_machine");

                if (screenMachine == null)
                {
                    _logger.LogError("Screen machine not found in Hand service response");
                    return false;
                }

                // Parse material ratio (e.g., "sand:copper" or "2:1")
                var (sandKg, copperKg) = ParseMaterialRatio(screenMachine.MaterialRatio);
                var outputScreensPerDay = screenMachine.ProductionRate;

                _logger.LogInformation("Found screen machine - Sand: {SandKg}kg, Copper: {CopperKg}kg, Output: {OutputScreens} screens/day",
                    sandKg, copperKg, outputScreensPerDay);

                // Initialize equipment parameters
                var success = await equipmentService.InitializeEquipmentParametersAsync(sandKg, copperKg, outputScreensPerDay);

                if (success)
                {
                    _logger.LogInformation("Equipment parameters successfully initialized from Hand service");
                }
                else
                {
                    _logger.LogError("Failed to save equipment parameters to database");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing equipment parameters from Hand service");
                return false;
            }
        }

        private (int sandKg, int copperKg) ParseMaterialRatio(string materialRatio)
        {
            try
            {
                // Handle formats like "sand:copper", "2:1", etc.
                var parts = materialRatio.Split(':');
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Invalid material ratio format: {MaterialRatio}. Using default 1:1", materialRatio);
                    return (1, 1);
                }

                // Try to parse as numbers first (e.g., "2:1")
                if (int.TryParse(parts[0].Trim(), out int sandRatio) &&
                    int.TryParse(parts[1].Trim(), out int copperRatio))
                {
                    return (sandRatio, copperRatio);
                }
                return (1, 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing material ratio: {MaterialRatio}. Using default 1:1", materialRatio);
                return (1, 1);
            }
        }

        public int GetCurrentSimulationDay()
        {
            if (!_simulationRunning) return 0;

            var currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var elapsedRealSeconds = currentUnixTime - _simulationStartUnixEpoch;
            var elapsedSimDays = (int)(elapsedRealSeconds / 120); // 120 seconds = 1 sim day

            return Math.Max(0, elapsedSimDays);
        }

        public DateTime GetSimulationDateTime()
        {
            var simDay = GetCurrentSimulationDay();
            return new DateTime(2050, 1, 1).AddDays(simDay);
        }

        public bool IsSimulationRunning() => _simulationRunning;

        public TimeSpan GetTimeUntilNextDay()
        {
            if (!_simulationRunning) return TimeSpan.Zero;

            var currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var elapsedRealSeconds = currentUnixTime - _simulationStartUnixEpoch;
            var secondsIntoCurrentDay = elapsedRealSeconds % 120;
            var secondsUntilNextDay = 120 - secondsIntoCurrentDay;

            return TimeSpan.FromSeconds(secondsUntilNextDay);
        }

        private async void ProcessDayTransition(object? state)
        {
            if (!_simulationRunning) return;

            try
            {
                var currentDay = GetCurrentSimulationDay();
                var previousDay = currentDay - 1;

                if (previousDay >= 0)
                {
                    await TriggerEndOfDay(previousDay);
                }

                await TriggerStartOfDay(currentDay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing day transition");
            }
        }

        private async Task TriggerStartOfDay(int day)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var equipmentService = scope.ServiceProvider.GetRequiredService<EquipmentService>();
                var reorderService = scope.ServiceProvider.GetRequiredService<ReorderService>();
                var bankIntegrationService = scope.ServiceProvider.GetRequiredService<BankIntegrationService>();
                var handService = scope.ServiceProvider.GetRequiredService<HandService>();

                var simDate = new DateTime(2050, 1, 1).AddDays(day);
                _logger.LogInformation("START OF DAY {Day} ({SimDate:yyyy-MM-dd})", day, simDate);

                if (!_bankAccountCreated || !_bankLoanCreated || !_notificationUrlSet)
                {
                    await bankIntegrationService.InitializeAsync(_bankAccountCreated, _bankLoanCreated, _notificationUrlSet);
                }

                if (!_equipmentParametersInitialized)
                {
                    _equipmentParametersInitialized = await InitializeEquipmentParametersFromHand(
                        handService, equipmentService);
                }

                // Log current inventory status
                await LogInventoryStatus(scope.ServiceProvider);

                // Start production if materials are available
                var machinesStarted = await equipmentService.StartProductionAsync();
                await reorderService.CheckAndProcessReordersAsync();

                if (machinesStarted > 0)
                {
                    _logger.LogInformation("Production started on {MachineCount} machines for day {Day}", machinesStarted, day);
                }
                else
                {
                    _logger.LogWarning("No machines started production on day {Day} - check material availability", day);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in start of day {Day}", day);
            }
        }

        private async Task TriggerEndOfDay(int day)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var equipmentService = scope.ServiceProvider.GetRequiredService<EquipmentService>();
                var productService = scope.ServiceProvider.GetRequiredService<ProductService>();

                var simDate = new DateTime(2050, 1, 1).AddDays(day);
                _logger.LogInformation("END OF DAY {Day} ({SimDate:yyyy-MM-dd})", day, simDate);

                // Stop production and collect output
                var screensProduced = await equipmentService.StopProductionAsync();

                if (screensProduced > 0)
                {
                    _logger.LogInformation("Day {Day} production complete: {ScreensProduced} screens produced", day, screensProduced);
                }
                else
                {
                    _logger.LogInformation("Day {Day} production complete: No screens produced", day);
                }

                // Update pricing based on current costs
                await productService.UpdateUnitPriceAsync();

                // Log end-of-day summary
                await LogDailySummary(scope.ServiceProvider, day, screensProduced);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in end of day {Day}", day);
            }
        }

        private async Task LogInventoryStatus(IServiceProvider serviceProvider)
        {
            try
            {
                var materialService = serviceProvider.GetRequiredService<MaterialService>();
                var productService = serviceProvider.GetRequiredService<ProductService>();
                var equipmentService = serviceProvider.GetRequiredService<EquipmentService>();

                var materials = await materialService.GetAllMaterialsAsync();
                var (totalScreens, reservedScreens, availableScreens) = await productService.GetStockSummaryAsync();
                var allEquipment = await equipmentService.GetAllEquipmentAsync();

                _logger.LogInformation("INVENTORY STATUS:");

                foreach (var material in materials)
                {
                    _logger.LogInformation("   {MaterialName}: {Quantity}kg", material.Name, material.Quantity);
                }

                _logger.LogInformation("   Equipment: {TotalCount} machines ({AvailableCount} available, {ProducingCount} producing)",
                    allEquipment.Count,
                    allEquipment.Count(e => e.IsAvailable && !e.IsProducing),
                    allEquipment.Count(e => e.IsProducing));

                _logger.LogInformation("   Screens: {TotalScreens} total, {ReservedScreens} reserved, {AvailableScreens} available",
                    totalScreens, reservedScreens, availableScreens);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not log inventory status");
            }
        }

        private async Task LogDailySummary(IServiceProvider serviceProvider, int day, int screensProduced)
        {
            try
            {
                var bankService = serviceProvider.GetRequiredService<BankService>();
                var productService = serviceProvider.GetRequiredService<ProductService>();

                try
                {
                    var balance = await bankService.GetAccountBalanceAsync();
                    _logger.LogInformation("Account balance: {Balance}", balance);
                }
                catch
                {
                    _logger.LogInformation("Account balance: Not available");
                }

                var (totalScreens, reservedScreens, availableScreens) = await productService.GetStockSummaryAsync();
                var product = await productService.GetProductAsync();
                var currentPrice = product?.Price ?? 0;

                _logger.LogInformation("DAY {Day} SUMMARY:", day);
                _logger.LogInformation("   Screens produced: {ScreensProduced}", screensProduced);
                _logger.LogInformation("   Total inventory: {TotalScreens} screens", totalScreens);
                _logger.LogInformation("   Available for sale: {AvailableScreens} screens", availableScreens);
                _logger.LogInformation("   Current unit price: {UnitPrice}", currentPrice);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not log daily summary");
            }
        }

        public void StopSimulation()
        {
            if (!_simulationRunning) return;

            _simulationRunning = false;
            _dayTimer?.Dispose();
            _dayTimer = null;

            var finalDay = GetCurrentSimulationDay();
            _logger.LogInformation("Simulation stopped at day {Day}", finalDay);
        }

        private async Task CleanUpDatabase(ScreenContext context)
        {
            // Clear Tables
            context.Equipment.ExecuteDelete();
            context.EquipmentParameters.ExecuteDelete();
            context.BankDetails.ExecuteDelete();
            context.PurchaseOrders.ExecuteDelete();
            context.ScreenOrders.ExecuteDelete();

            // Reset others
            await context.Products.ForEachAsync(p => {
                p.Price = 0;
                p.Quantity = 0;
            });
            await context.Materials.ForEachAsync(p =>
            {
                p.Quantity = 0;
            });

            await context.SaveChangesAsync();

            _equipmentParametersInitialized = false;
            _bankAccountCreated = false;
            _bankLoanCreated = false;
            _notificationUrlSet = false;
            return;
        }

        public async Task DestroySimulation()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();
          
            StopSimulation();
            await CleanUpDatabase(context);
        }

        public void Dispose()
        {
            StopSimulation();
        }
    }
}