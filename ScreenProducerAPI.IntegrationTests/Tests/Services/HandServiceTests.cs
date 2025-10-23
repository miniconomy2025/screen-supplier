using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.IntegrationTests.Fixtures;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;

namespace ScreenProducerAPI.IntegrationTests.Tests.Services;

[TestFixture]
public class HandServiceTests
{
    private CustomWebApplicationFactory _factory = null!;
    private IHandService _handService = null!;
    private ILogger<HandServiceTests> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new CustomWebApplicationFactory();
        var scope = _factory.Services.CreateScope();
        _handService = scope.ServiceProvider.GetRequiredService<IHandService>();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<HandServiceTests>>();

        TestContext.WriteLine($"Starting test: {TestContext.CurrentContext.Test.Name}");
        TestContext.WriteLine($"Test description: {TestContext.CurrentContext.Test.Properties.Get("Description") ?? "No description provided"}");
    }

    [TearDown]
    public void TearDown()
    {
        var result = TestContext.CurrentContext.Result.Outcome.Status;
        var testName = TestContext.CurrentContext.Test.Name;

        TestContext.WriteLine($"Test '{testName}' completed with status: {result}");

        if (result == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            TestContext.WriteLine($"Test failure message: {TestContext.CurrentContext.Result.Message}");
            TestContext.WriteLine($"Stack trace: {TestContext.CurrentContext.Result.StackTrace}");
        }

        _factory?.Dispose();
    }

    [Test]
    [Description("Verifies that GetMachinesForSaleAsync returns valid machine data with all required properties")]
    public async Task GetMachinesForSaleAsync_Returns_Expected_Machines()
    {
        TestContext.WriteLine("Testing GetMachinesForSaleAsync for expected machine data structure");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _handService.GetMachinesForSaleAsync();
        stopwatch.Stop();

        TestContext.WriteLine($"API call completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Received {response?.Machines?.Count ?? 0} machines");

        // Assert
        response.Should().NotBeNull();
        response.Machines.Should().NotBeNullOrEmpty();
        response.Machines.Should().HaveCountGreaterThan(0);

        var machine = response.Machines.First();
        TestContext.WriteLine($"First machine details: Name='{machine.MachineName}', Quantity={machine.Quantity}, Price={machine.Price}");

        machine.MachineName.Should().NotBeNullOrEmpty();
        machine.Quantity.Should().BeGreaterThan(0);
        machine.Price.Should().BeGreaterThan(0);
        machine.ProductionRate.Should().BeGreaterThan(0);
        machine.Weight.Should().BeGreaterThan(0);
        machine.InputRatio.Should().NotBeNull();
        machine.InputRatio.Copper.Should().BeGreaterThan(0);
        machine.InputRatio.Sand.Should().BeGreaterThan(0);

        TestContext.WriteLine($"Machine validation successful: Copper ratio={machine.InputRatio.Copper}, Sand ratio={machine.InputRatio.Sand}");
    }

    [Test]
    [Description("Ensures GetMachinesForSaleAsync returns consistent data across multiple calls")]
    public async Task GetMachinesForSaleAsync_Returns_Consistent_Data()
    {
        TestContext.WriteLine("Testing GetMachinesForSaleAsync for data consistency across multiple calls");

        // Act
        var firstResponse = await _handService.GetMachinesForSaleAsync();
        var secondResponse = await _handService.GetMachinesForSaleAsync();

        TestContext.WriteLine($"First call returned {firstResponse?.Machines?.Count ?? 0} machines");
        TestContext.WriteLine($"Second call returned {secondResponse?.Machines?.Count ?? 0} machines");

        // Assert
        firstResponse.Should().NotBeNull();
        secondResponse.Should().NotBeNull();
        firstResponse.Machines.Should().HaveCount(secondResponse.Machines.Count);

        for (int i = 0; i < firstResponse.Machines.Count; i++)
        {
            var firstMachine = firstResponse.Machines[i];
            var secondMachine = secondResponse.Machines[i];

            TestContext.WriteLine($"Comparing machine {i}: '{firstMachine.MachineName}' vs '{secondMachine.MachineName}'");

            firstMachine.MachineName.Should().Be(secondMachine.MachineName);
            firstMachine.Price.Should().Be(secondMachine.Price);
            firstMachine.ProductionRate.Should().Be(secondMachine.ProductionRate);
            firstMachine.Weight.Should().Be(secondMachine.Weight);
        }

        TestContext.WriteLine("Data consistency validation successful");
    }

    [Test]
    [Description("Verifies that GetRawMaterialsForSaleAsync returns valid material data")]
    public async Task GetRawMaterialsForSaleAsync_Returns_Expected_Materials()
    {
        TestContext.WriteLine("Testing GetRawMaterialsForSaleAsync for expected material data");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _handService.GetRawMaterialsForSaleAsync();
        stopwatch.Stop();

        TestContext.WriteLine($"API call completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Received {response?.Count ?? 0} materials");

        // Assert
        response.Should().NotBeNull();
        response.Should().NotBeEmpty();
        response.Should().HaveCountGreaterThan(0);

        foreach (var material in response)
        {
            TestContext.WriteLine($"Material: '{material.RawMaterialName}', Price per kg: {material.PricePerKg}, Available: {material.QuantityAvailable}");

            material.RawMaterialName.Should().NotBeNullOrEmpty();
            material.PricePerKg.Should().BeGreaterThan(0);
            material.QuantityAvailable.Should().BeGreaterThan(0);
        }

        TestContext.WriteLine("Material validation successful");
    }

    [Test]
    [Description("Ensures GetRawMaterialsForSaleAsync includes expected materials (sand and copper)")]
    public async Task GetRawMaterialsForSaleAsync_Contains_Expected_Materials()
    {
        TestContext.WriteLine("Testing GetRawMaterialsForSaleAsync for presence of expected materials");

        // Act
        var response = await _handService.GetRawMaterialsForSaleAsync();

        // Assert
        response.Should().NotBeNull();

        var materialNames = response.Select(m => m.RawMaterialName.ToLowerInvariant()).ToList();
        TestContext.WriteLine($"Available materials: {string.Join(", ", materialNames)}");

        materialNames.Should().Contain("sand");
        materialNames.Should().Contain("copper");

        TestContext.WriteLine("Expected materials validation successful");
    }

    [Test]
    [Description("Verifies that PurchaseMachineAsync processes valid requests correctly")]
    public async Task PurchaseMachineAsync_With_Valid_Request_Returns_Success()
    {
        TestContext.WriteLine("Testing PurchaseMachineAsync with valid request");

        // Arrange
        var request = new PurchaseMachineRequest
        {
            MachineName = "Test Machine",
            Quantity = 1
        };

        TestContext.WriteLine($"Purchasing machine: '{request.MachineName}', Quantity: {request.Quantity}");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _handService.PurchaseMachineAsync(request);
        stopwatch.Stop();

        TestContext.WriteLine($"Purchase completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Order ID: {response.OrderId}, Total Price: {response.TotalPrice}, Bank Account: {response.BankAccount}");

        // Assert
        response.Should().NotBeNull();
        response.OrderId.Should().BeGreaterThan(0);
        response.MachineName.Should().NotBeNullOrEmpty();
        response.Quantity.Should().Be(1);
        response.TotalPrice.Should().BeGreaterThan(0);
        response.UnitWeight.Should().BeGreaterThan(0);
        response.BankAccount.Should().NotBeNullOrEmpty();

        TestContext.WriteLine("Machine purchase validation successful");
    }

    [Test]
    [Description("Tests PurchaseMachineAsync with multiple quantities and validates price scaling")]
    public async Task PurchaseMachineAsync_With_Multiple_Quantity_Returns_Correct_Data()
    {
        TestContext.WriteLine("Testing PurchaseMachineAsync with multiple quantities");

        // Arrange
        var request = new PurchaseMachineRequest
        {
            MachineName = "Test Machine",
            Quantity = 3
        };

        TestContext.WriteLine($"Purchasing machines: '{request.MachineName}', Quantity: {request.Quantity}");

        // Act
        var response = await _handService.PurchaseMachineAsync(request);

        TestContext.WriteLine($"Order ID: {response.OrderId}, Total Price: {response.TotalPrice}, Quantity: {response.Quantity}");

        // Assert
        response.Should().NotBeNull();
        response.OrderId.Should().BeGreaterThan(0);
        response.MachineName.Should().NotBeNullOrEmpty();
        response.Quantity.Should().Be(3);
        response.TotalPrice.Should().BeGreaterThan(0);
        response.UnitWeight.Should().BeGreaterThan(0);
        response.BankAccount.Should().NotBeNullOrEmpty();

        TestContext.WriteLine("Multiple quantity purchase validation successful");
    }

    [Test]
    [Description("Verifies that PurchaseMachineAsync generates unique order IDs for each request")]
    public async Task PurchaseMachineAsync_Generates_Unique_OrderIds()
    {
        TestContext.WriteLine("Testing PurchaseMachineAsync for unique order ID generation");

        // Arrange
        var request1 = new PurchaseMachineRequest
        {
            MachineName = "Test Machine",
            Quantity = 1
        };

        var request2 = new PurchaseMachineRequest
        {
            MachineName = "Test Machine",
            Quantity = 1
        };

        // Act
        var response1 = await _handService.PurchaseMachineAsync(request1);
        var response2 = await _handService.PurchaseMachineAsync(request2);

        TestContext.WriteLine($"First order ID: {response1.OrderId}");
        TestContext.WriteLine($"Second order ID: {response2.OrderId}");

        // Assert
        response1.Should().NotBeNull();
        response2.Should().NotBeNull();
        response1.OrderId.Should().NotBe(response2.OrderId);

        TestContext.WriteLine("Unique order ID validation successful");
    }

    [Test]
    [Description("Tests PurchaseRawMaterialAsync with valid request and validates response")]
    public async Task PurchaseRawMaterialAsync_With_Valid_Request_Returns_Success()
    {
        TestContext.WriteLine("Testing PurchaseRawMaterialAsync with valid request");

        // Arrange
        var request = new PurchaseRawMaterialRequest
        {
            MaterialName = "Sand",
            WeightQuantity = 100.5m
        };

        TestContext.WriteLine($"Purchasing material: '{request.MaterialName}', Weight: {request.WeightQuantity}kg");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _handService.PurchaseRawMaterialAsync(request);
        stopwatch.Stop();

        TestContext.WriteLine($"Purchase completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Order ID: {response.OrderId}, Price: {response.Price}, Bank Account: {response.BankAccount}");

        // Assert
        response.Should().NotBeNull();
        response.OrderId.Should().BeGreaterThan(0);
        response.MaterialName.Should().NotBeNullOrEmpty();
        response.WeightQuantity.Should().Be(100.5m);
        response.Price.Should().BeGreaterThan(0);
        response.BankAccount.Should().NotBeNullOrEmpty();

        TestContext.WriteLine("Material purchase validation successful");
    }

    [Test]
    [Description("Tests PurchaseRawMaterialAsync with different materials and validates unique responses")]
    public async Task PurchaseRawMaterialAsync_With_Different_Materials_Returns_Different_Responses()
    {
        TestContext.WriteLine("Testing PurchaseRawMaterialAsync with different materials");

        // Arrange
        var sandRequest = new PurchaseRawMaterialRequest
        {
            MaterialName = "Sand",
            WeightQuantity = 100m
        };

        var copperRequest = new PurchaseRawMaterialRequest
        {
            MaterialName = "Copper",
            WeightQuantity = 100m
        };

        // Act
        var sandResponse = await _handService.PurchaseRawMaterialAsync(sandRequest);
        var copperResponse = await _handService.PurchaseRawMaterialAsync(copperRequest);

        TestContext.WriteLine($"Sand order - ID: {sandResponse.OrderId}, Price: {sandResponse.Price}");
        TestContext.WriteLine($"Copper order - ID: {copperResponse.OrderId}, Price: {copperResponse.Price}");

        // Assert
        sandResponse.Should().NotBeNull();
        copperResponse.Should().NotBeNull();
        sandResponse.OrderId.Should().NotBe(copperResponse.OrderId);
        sandResponse.MaterialName.Should().NotBe(copperResponse.MaterialName);

        TestContext.WriteLine("Different materials validation successful");
    }

    [Test]
    [Description("Verifies that PurchaseRawMaterialAsync generates unique order IDs")]
    public async Task PurchaseRawMaterialAsync_Generates_Unique_OrderIds()
    {
        TestContext.WriteLine("Testing PurchaseRawMaterialAsync for unique order ID generation");

        // Arrange
        var request1 = new PurchaseRawMaterialRequest
        {
            MaterialName = "Sand",
            WeightQuantity = 50m
        };

        var request2 = new PurchaseRawMaterialRequest
        {
            MaterialName = "Sand",
            WeightQuantity = 50m
        };

        // Act
        var response1 = await _handService.PurchaseRawMaterialAsync(request1);
        var response2 = await _handService.PurchaseRawMaterialAsync(request2);

        TestContext.WriteLine($"First order ID: {response1.OrderId}");
        TestContext.WriteLine($"Second order ID: {response2.OrderId}");

        // Assert
        response1.Should().NotBeNull();
        response2.Should().NotBeNull();
        response1.OrderId.Should().NotBe(response2.OrderId);

        TestContext.WriteLine("Unique order ID validation successful");
    }

    [Test]
    [Description("Tests GetCurrentSimulationTimeAsync and validates time format")]
    public async Task GetCurrentSimulationTimeAsync_Returns_Valid_Time()
    {
        TestContext.WriteLine("Testing GetCurrentSimulationTimeAsync for valid time format");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _handService.GetCurrentSimulationTimeAsync();
        stopwatch.Stop();

        TestContext.WriteLine($"API call completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Received date: '{response.Date}', time: '{response.Time}'");

        // Assert
        response.Should().NotBeNull();
        response.Date.Should().NotBeNullOrEmpty();
        response.Time.Should().NotBeNullOrEmpty();

        // Validate date format (should be parseable)
        var dateParseResult = DateTime.TryParse(response.Date, out var parsedDate);
        var timeParseResult = TimeSpan.TryParse(response.Time, out var parsedTime);

        TestContext.WriteLine($"Date parse result: {dateParseResult}, parsed date: {parsedDate}");
        TestContext.WriteLine($"Time parse result: {timeParseResult}, parsed time: {parsedTime}");

        dateParseResult.Should().BeTrue("Date should be in a valid format");
        timeParseResult.Should().BeTrue("Time should be in a valid format");

        TestContext.WriteLine("Time format validation successful");
    }

    [Test]
    [Description("Validates GetCurrentSimulationTimeAsync returns time close to current time")]
    public async Task GetCurrentSimulationTimeAsync_Returns_Current_Time()
    {
        TestContext.WriteLine("Testing GetCurrentSimulationTimeAsync for current time accuracy");

        // Act
        var beforeCall = DateTime.UtcNow;
        var response = await _handService.GetCurrentSimulationTimeAsync();
        var afterCall = DateTime.UtcNow;

        TestContext.WriteLine($"Call window: {beforeCall:yyyy-MM-dd HH:mm:ss} to {afterCall:yyyy-MM-dd HH:mm:ss}");
        TestContext.WriteLine($"Response date: '{response.Date}', time: '{response.Time}'");

        // Assert
        response.Should().NotBeNull();

        var parsedDate = DateTime.Parse(response.Date);
        var parsedTime = TimeSpan.Parse(response.Time);
        var combinedDateTime = parsedDate.Add(parsedTime);

        TestContext.WriteLine($"Combined response time: {combinedDateTime:yyyy-MM-dd HH:mm:ss}");
        TestContext.WriteLine($"Time difference from UTC now: {Math.Abs((combinedDateTime - DateTime.UtcNow).TotalMinutes):F2} minutes");

        // Should be close to current time (within 5 minutes for test flexibility)
        combinedDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

        TestContext.WriteLine("Current time accuracy validation successful");
    }

    [Test]
    [Description("Tests GetSimulationStatusAsync and validates status response")]
    public async Task GetSimulationStatusAsync_Returns_Valid_Status()
    {
        TestContext.WriteLine("Testing GetSimulationStatusAsync for valid status");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _handService.GetSimulationStatusAsync();
        stopwatch.Stop();

        TestContext.WriteLine($"API call completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Status - Online: {response?.isOnline}, Running: {response?.IsRunning}, Epoch: {response?.EpochStartTime}");

        // Assert
        response.Should().NotBeNull();
        response!.EpochStartTime.Should().BeGreaterThan(0);
        // IsRunning and isOnline can be true or false, both are valid

        TestContext.WriteLine("Simulation status validation successful");
    }

    [Test]
    [Description("Ensures GetSimulationStatusAsync returns consistent data across calls")]
    public async Task GetSimulationStatusAsync_Returns_Consistent_Data()
    {
        TestContext.WriteLine("Testing GetSimulationStatusAsync for data consistency");

        // Act
        var firstResponse = await _handService.GetSimulationStatusAsync();
        var secondResponse = await _handService.GetSimulationStatusAsync();

        TestContext.WriteLine($"First call - Online: {firstResponse?.isOnline}, Running: {firstResponse?.IsRunning}, Epoch: {firstResponse?.EpochStartTime}");
        TestContext.WriteLine($"Second call - Online: {secondResponse?.isOnline}, Running: {secondResponse?.IsRunning}, Epoch: {secondResponse?.EpochStartTime}");

        // Assert
        firstResponse.Should().NotBeNull();
        secondResponse.Should().NotBeNull();

        // Epoch start time should be consistent between calls
        firstResponse!.EpochStartTime.Should().Be(secondResponse!.EpochStartTime);
        firstResponse.IsRunning.Should().Be(secondResponse.IsRunning);
        firstResponse.isOnline.Should().Be(secondResponse.isOnline);

        TestContext.WriteLine("Status consistency validation successful");
    }

    [Test]
    [Description("Tests TryInitializeEquipmentParametersAsync with valid equipment service")]
    public async Task TryInitializeEquipmentParametersAsync_Returns_Success()
    {
        TestContext.WriteLine("Testing TryInitializeEquipmentParametersAsync with valid equipment service");

        // Arrange
        using var scope = _factory.Services.CreateScope();
        var equipmentService = scope.ServiceProvider.GetRequiredService<IEquipmentService>();

        TestContext.WriteLine($"Equipment service type: {equipmentService.GetType().Name}");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _handService.TryInitializeEquipmentParametersAsync(equipmentService);
        stopwatch.Stop();

        TestContext.WriteLine($"Initialization completed in {stopwatch.ElapsedMilliseconds}ms with result: {result}");

        // Assert
        result.Should().BeTrue();

        TestContext.WriteLine("Equipment initialization validation successful");
    }

    [Test]
    [Description("Tests TryInitializeEquipmentParametersAsync with null equipment service")]
    public async Task TryInitializeEquipmentParametersAsync_With_Null_EquipmentService_Handles_Gracefully()
    {
        TestContext.WriteLine("Testing TryInitializeEquipmentParametersAsync with null equipment service");

        // Act
        var result = await _handService.TryInitializeEquipmentParametersAsync(null!);

        TestContext.WriteLine($"Initialization with null service result: {result}");

        // Assert
        // Should not throw exception and should return false or handle gracefully
        result.Should().BeFalse();

        TestContext.WriteLine("Null equipment service handling validation successful");
    }

    [Test]
    [Description("Tests thread safety of HandService methods with concurrent operations")]
    public async Task HandService_Methods_Are_Thread_Safe()
    {
        TestContext.WriteLine("Testing HandService methods for thread safety");

        // Arrange
        var tasks = new List<Task>();
        var taskNames = new List<string>();

        // Act - Run multiple operations concurrently
        tasks.Add(_handService.GetMachinesForSaleAsync());
        taskNames.Add("GetMachinesForSaleAsync");

        tasks.Add(_handService.GetRawMaterialsForSaleAsync());
        taskNames.Add("GetRawMaterialsForSaleAsync");

        tasks.Add(_handService.GetCurrentSimulationTimeAsync());
        taskNames.Add("GetCurrentSimulationTimeAsync");

        tasks.Add(_handService.GetSimulationStatusAsync());
        taskNames.Add("GetSimulationStatusAsync");

        tasks.Add(_handService.PurchaseMachineAsync(new PurchaseMachineRequest { MachineName = "Test", Quantity = 1 }));
        taskNames.Add("PurchaseMachineAsync");

        tasks.Add(_handService.PurchaseRawMaterialAsync(new PurchaseRawMaterialRequest { MaterialName = "Sand", WeightQuantity = 10m }));
        taskNames.Add("PurchaseRawMaterialAsync");

        TestContext.WriteLine($"Starting {tasks.Count} concurrent operations");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Assert - All tasks should complete without throwing exceptions
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        TestContext.WriteLine($"All concurrent operations completed in {stopwatch.ElapsedMilliseconds}ms");

        for (int i = 0; i < tasks.Count; i++)
        {
            TestContext.WriteLine($"Task {taskNames[i]}: {(tasks[i].IsCompletedSuccessfully ? "SUCCESS" : "FAILED")}");
            tasks[i].IsCompletedSuccessfully.Should().BeTrue($"{taskNames[i]} should complete successfully");
        }

        TestContext.WriteLine("Thread safety validation successful");
    }

    [Test]
    [Description("Tests multiple consecutive calls to verify service reliability")]
    public async Task HandService_Multiple_Consecutive_Calls_Work_Correctly()
    {
        TestContext.WriteLine("Testing HandService with multiple consecutive calls");

        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act & Assert - Multiple consecutive calls should work without issues
        for (int i = 0; i < 5; i++)
        {
            TestContext.WriteLine($"--- Iteration {i + 1} ---");

            var iterationStopwatch = System.Diagnostics.Stopwatch.StartNew();

            var machinesResponse = await _handService.GetMachinesForSaleAsync();
            machinesResponse.Should().NotBeNull();
            machinesResponse.Machines.Should().NotBeEmpty();
            TestContext.WriteLine($"GetMachinesForSaleAsync: {machinesResponse.Machines.Count} machines");

            var materialsResponse = await _handService.GetRawMaterialsForSaleAsync();
            materialsResponse.Should().NotBeNull();
            materialsResponse.Should().NotBeEmpty();
            TestContext.WriteLine($"GetRawMaterialsForSaleAsync: {materialsResponse.Count} materials");

            var timeResponse = await _handService.GetCurrentSimulationTimeAsync();
            timeResponse.Should().NotBeNull();
            timeResponse.Date.Should().NotBeNullOrEmpty();
            TestContext.WriteLine($"GetCurrentSimulationTimeAsync: {timeResponse.Date} {timeResponse.Time}");

            var statusResponse = await _handService.GetSimulationStatusAsync();
            statusResponse.Should().NotBeNull();
            TestContext.WriteLine($"GetSimulationStatusAsync: Online={statusResponse.isOnline}, Running={statusResponse.IsRunning}");

            iterationStopwatch.Stop();
            TestContext.WriteLine($"Iteration {i + 1} completed in {iterationStopwatch.ElapsedMilliseconds}ms");
        }

        totalStopwatch.Stop();
        TestContext.WriteLine($"All 5 iterations completed in {totalStopwatch.ElapsedMilliseconds}ms (avg: {totalStopwatch.ElapsedMilliseconds / 5}ms per iteration)");

        TestContext.WriteLine("Multiple consecutive calls validation successful");
    }

    [Test]
    [Description("Tests complete purchase workflow from machine discovery to material purchase")]
    public async Task HandService_Workflow_Complete_Purchase_Cycle()
    {
        TestContext.WriteLine("Testing complete HandService purchase workflow");

        var workflowStopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Step 1: Get available machines
        TestContext.WriteLine("Step 1: Getting available machines...");
        var machinesResponse = await _handService.GetMachinesForSaleAsync();
        machinesResponse.Should().NotBeNull();
        machinesResponse.Machines.Should().NotBeEmpty();
        TestContext.WriteLine($"Found {machinesResponse.Machines.Count} available machines");

        // Step 2: Get available materials
        TestContext.WriteLine("Step 2: Getting available materials...");
        var materialsResponse = await _handService.GetRawMaterialsForSaleAsync();
        materialsResponse.Should().NotBeNull();
        materialsResponse.Should().NotBeEmpty();
        TestContext.WriteLine($"Found {materialsResponse.Count} available materials");

        // Step 3: Purchase a machine
        TestContext.WriteLine("Step 3: Purchasing a machine...");
        var machineRequest = new PurchaseMachineRequest
        {
            MachineName = machinesResponse.Machines.First().MachineName,
            Quantity = 1
        };
        var machinePurchaseResponse = await _handService.PurchaseMachineAsync(machineRequest);
        machinePurchaseResponse.Should().NotBeNull();
        machinePurchaseResponse.OrderId.Should().BeGreaterThan(0);
        TestContext.WriteLine($"Machine purchase successful - Order ID: {machinePurchaseResponse.OrderId}, Price: {machinePurchaseResponse.TotalPrice}");

        // Step 4: Purchase materials
        TestContext.WriteLine("Step 4: Purchasing materials...");
        var materialRequest = new PurchaseRawMaterialRequest
        {
            MaterialName = materialsResponse.First().RawMaterialName,
            WeightQuantity = 100m
        };
        var materialPurchaseResponse = await _handService.PurchaseRawMaterialAsync(materialRequest);
        materialPurchaseResponse.Should().NotBeNull();
        materialPurchaseResponse.OrderId.Should().BeGreaterThan(0);
        TestContext.WriteLine($"Material purchase successful - Order ID: {materialPurchaseResponse.OrderId}, Price: {materialPurchaseResponse.Price}");

        // Step 5: Check simulation status
        TestContext.WriteLine("Step 5: Checking simulation status...");
        var statusResponse = await _handService.GetSimulationStatusAsync();
        statusResponse.Should().NotBeNull();
        TestContext.WriteLine($"Simulation status - Online: {statusResponse.isOnline}, Running: {statusResponse.IsRunning}");

        // Step 6: Get current time
        TestContext.WriteLine("Step 6: Getting current simulation time...");
        var timeResponse = await _handService.GetCurrentSimulationTimeAsync();
        timeResponse.Should().NotBeNull();
        TestContext.WriteLine($"Current simulation time: {timeResponse.Date} {timeResponse.Time}");

        // All operations should complete successfully
        machinePurchaseResponse.OrderId.Should().NotBe(materialPurchaseResponse.OrderId);

        workflowStopwatch.Stop();
        TestContext.WriteLine($"Complete workflow finished in {workflowStopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine("Complete purchase workflow validation successful");
    }

    [Test]
    [Description("Ensures PurchaseMachineAsync throws InsufficientStockException for machines with insufficient stock")]
    public async Task PurchaseMachineAsync_With_Insufficient_Stock_Throws_InsufficientStockException()
    {
        // Arrange
        var request = new PurchaseMachineRequest
        {
            MachineName = "INSUFFICIENT_STOCK_MACHINE",
            Quantity = 1
        };

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _handService.PurchaseMachineAsync(request))
            .Should().ThrowAsync<InsufficientStockException>();

        exception.Which.Message.Should().Contain("machines");
        exception.Which.Message.Should().Contain("1");
        exception.Which.Message.Should().Contain("0");
    }

    [Test]
    [Description("Ensures PurchaseMachineAsync throws HandServiceException on network error")]
    public async Task PurchaseMachineAsync_With_Network_Error_Throws_HandServiceException()
    {
        // Arrange
        var request = new PurchaseMachineRequest
        {
            MachineName = "NETWORK_ERROR_MACHINE",
            Quantity = 1
        };

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _handService.PurchaseMachineAsync(request))
            .Should().ThrowAsync<HandServiceException>();

        exception.Which.Message.Should().Contain("Hand service unavailable for machine purchase");
        exception.Which.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Test]
    [Description("Ensures PurchaseMachineAsync throws HandServiceException on timeout")]
    public async Task PurchaseMachineAsync_With_Timeout_Throws_HandServiceException()
    {
        // Arrange
        var request = new PurchaseMachineRequest
        {
            MachineName = "TIMEOUT_MACHINE",
            Quantity = 1
        };

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _handService.PurchaseMachineAsync(request))
            .Should().ThrowAsync<HandServiceException>();

        exception.Which.Message.Should().Contain("Hand service timeout during machine purchase");
        exception.Which.InnerException.Should().BeOfType<TaskCanceledException>();
    }

    [Test]
    [Description("Ensures PurchaseRawMaterialAsync throws InsufficientStockException for materials with insufficient stock")]
    public async Task PurchaseRawMaterialAsync_With_Insufficient_Stock_Throws_InsufficientStockException()
    {
        // Arrange
        var request = new PurchaseRawMaterialRequest
        {
            MaterialName = "INSUFFICIENT_STOCK_MATERIAL",
            WeightQuantity = 100m
        };

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _handService.PurchaseRawMaterialAsync(request))
            .Should().ThrowAsync<InsufficientStockException>();

        exception.Which.Message.Should().Contain("INSUFFICIENT_STOCK_MATERIAL");
        exception.Which.Message.Should().Contain("100");
        exception.Which.Message.Should().Contain("0");
    }

    [Test]
    [Description("Ensures PurchaseRawMaterialAsync throws HandServiceException on network error")]
    public async Task PurchaseRawMaterialAsync_With_Network_Error_Throws_HandServiceException()
    {
        // Arrange
        var request = new PurchaseRawMaterialRequest
        {
            MaterialName = "NETWORK_ERROR_MATERIAL",
            WeightQuantity = 50m
        };

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _handService.PurchaseRawMaterialAsync(request))
            .Should().ThrowAsync<HandServiceException>();

        exception.Which.Message.Should().Contain("Hand service unavailable for raw material purchase");
        exception.Which.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Test]
    [Description("Ensures PurchaseRawMaterialAsync throws HandServiceException on timeout")]
    public async Task PurchaseRawMaterialAsync_With_Timeout_Throws_HandServiceException()
    {
        // Arrange
        var request = new PurchaseRawMaterialRequest
        {
            MaterialName = "TIMEOUT_MATERIAL",
            WeightQuantity = 75m
        };

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _handService.PurchaseRawMaterialAsync(request))
            .Should().ThrowAsync<HandServiceException>();

        exception.Which.Message.Should().Contain("Hand service timeout during raw material purchase");
        exception.Which.InnerException.Should().BeOfType<TaskCanceledException>();
    }

    [Test]
    [Description("Verifies that PurchaseRawMaterialAsync calculates prices correctly")]
    public async Task PurchaseRawMaterialAsync_Price_Calculation_Is_Correct()
    {
        // Arrange
        var sandRequest = new PurchaseRawMaterialRequest
        {
            MaterialName = "Sand",
            WeightQuantity = 100m
        };

        var copperRequest = new PurchaseRawMaterialRequest
        {
            MaterialName = "Copper",
            WeightQuantity = 50m
        };

        // Act
        var sandResponse = await _handService.PurchaseRawMaterialAsync(sandRequest);
        var copperResponse = await _handService.PurchaseRawMaterialAsync(copperRequest);

        // Assert
        sandResponse.Should().NotBeNull();
        copperResponse.Should().NotBeNull();

        // Sand: 100kg * 10 = 1000, Copper: 50kg * 50 = 2500 (based on MockHandService pricing)
        sandResponse.Price.Should().Be(1000m);
        copperResponse.Price.Should().Be(2500m);
    }

    [Test]
    [Description("Checks that GetMachinesForSaleAsync includes 'screen_machine' in the available machines")]
    public async Task GetMachinesForSaleAsync_Contains_ScreenMachine()
    {
        // Act
        var response = await _handService.GetMachinesForSaleAsync();

        // Assert
        response.Should().NotBeNull();
        response.Machines.Should().NotBeEmpty();

        var screenMachine = response.Machines.FirstOrDefault(m => m.MachineName == "screen_machine");
        screenMachine.Should().NotBeNull("screen_machine should be available in the response");
        screenMachine!.InputRatio.Should().NotBeNull();
        screenMachine.InputRatio.Sand.Should().BeGreaterThan(0);
        screenMachine.InputRatio.Copper.Should().BeGreaterThan(0);
        screenMachine.ProductionRate.Should().BeGreaterThan(0);
    }

    [Test]
    [Description("Ensures TryInitializeEquipmentParametersAsync succeeds when 'screen_machine' is available")]
    public async Task TryInitializeEquipmentParametersAsync_With_ScreenMachine_Success()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var equipmentService = scope.ServiceProvider.GetRequiredService<IEquipmentService>();

        // Act
        var result = await _handService.TryInitializeEquipmentParametersAsync(equipmentService);

        // Assert
        result.Should().BeTrue("Equipment initialization should succeed when screen_machine is available");
    }

    [Test]
    [Description("Tests PurchaseMachineAsync price calculation with multiple quantities")]
    public async Task PurchaseMachineAsync_Price_Calculation_With_Multiple_Quantities()
    {
        // Arrange
        var singleRequest = new PurchaseMachineRequest
        {
            MachineName = "Test Machine",
            Quantity = 1
        };

        var multipleRequest = new PurchaseMachineRequest
        {
            MachineName = "Test Machine",
            Quantity = 3
        };

        // Act
        var singleResponse = await _handService.PurchaseMachineAsync(singleRequest);
        var multipleResponse = await _handService.PurchaseMachineAsync(multipleRequest);

        // Assert
        singleResponse.Should().NotBeNull();
        multipleResponse.Should().NotBeNull();

        // Price should scale with quantity (3x the single price)
        multipleResponse.TotalPrice.Should().Be(singleResponse.TotalPrice * 3);
        multipleResponse.Quantity.Should().Be(3);
        singleResponse.Quantity.Should().Be(1);
    }

    [Test]
    [Description("Verifies HandService exception handling does not affect subsequent calls")]
    public async Task HandService_Exception_Handling_Does_Not_Affect_Subsequent_Calls()
    {
        // Arrange & Act - First call that throws exception
        try
        {
            await _handService.PurchaseMachineAsync(new PurchaseMachineRequest
            {
                MachineName = "NETWORK_ERROR_MACHINE",
                Quantity = 1
            });
        }
        catch (HandServiceException)
        {
            // Expected exception, continue with test
        }

        // Act & Assert - Subsequent calls should work normally
        var machinesResponse = await _handService.GetMachinesForSaleAsync();
        machinesResponse.Should().NotBeNull();
        machinesResponse.Machines.Should().NotBeEmpty();

        var materialsResponse = await _handService.GetRawMaterialsForSaleAsync();
        materialsResponse.Should().NotBeNull();
        materialsResponse.Should().NotBeEmpty();

        var successfulPurchase = await _handService.PurchaseMachineAsync(new PurchaseMachineRequest
        {
            MachineName = "Test Machine",
            Quantity = 1
        });
        successfulPurchase.Should().NotBeNull();
        successfulPurchase.OrderId.Should().BeGreaterThan(0);
    }

    [Test]
    [Description("Ensures all HandService methods return non-null results")]
    public async Task HandService_All_Methods_Return_NonNull_Results()
    {
        // Act & Assert
        var machines = await _handService.GetMachinesForSaleAsync();
        machines.Should().NotBeNull("GetMachinesForSaleAsync should never return null");

        var materials = await _handService.GetRawMaterialsForSaleAsync();
        materials.Should().NotBeNull("GetRawMaterialsForSaleAsync should never return null");

        var simulationTime = await _handService.GetCurrentSimulationTimeAsync();
        simulationTime.Should().NotBeNull("GetCurrentSimulationTimeAsync should never return null");

        var simulationStatus = await _handService.GetSimulationStatusAsync();
        simulationStatus.Should().NotBeNull("GetSimulationStatusAsync should never return null");

        var machinePurchase = await _handService.PurchaseMachineAsync(new PurchaseMachineRequest
        {
            MachineName = "Test Machine",
            Quantity = 1
        });
        machinePurchase.Should().NotBeNull("PurchaseMachineAsync should never return null");

        var materialPurchase = await _handService.PurchaseRawMaterialAsync(new PurchaseRawMaterialRequest
        {
            MaterialName = "Sand",
            WeightQuantity = 10m
        });
        materialPurchase.Should().NotBeNull("PurchaseRawMaterialAsync should never return null");
    }
}