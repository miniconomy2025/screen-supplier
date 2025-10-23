using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.IntegrationTests.Fixtures;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.SupplierService.Recycler.Models;

namespace ScreenProducerAPI.IntegrationTests.Tests.Services;

[TestFixture]
public class RecyclerServiceTests
{
    private CustomWebApplicationFactory _factory = null!;
    private IRecyclerService _recyclerService = null!;
    private ILogger<RecyclerServiceTests> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new CustomWebApplicationFactory();
        var scope = _factory.Services.CreateScope();
        _recyclerService = scope.ServiceProvider.GetRequiredService<IRecyclerService>();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<RecyclerServiceTests>>();

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
    [Description("Verifies that GetMaterialsAsync returns valid material data with all required properties")]
    public async Task GetMaterialsAsync_Returns_Expected_Materials()
    {
        TestContext.WriteLine("Testing GetMaterialsAsync for expected material data structure");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _recyclerService.GetMaterialsAsync();
        stopwatch.Stop();

        TestContext.WriteLine($"API call completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Received {response?.Count ?? 0} materials");

        // Assert
        response.Should().NotBeNull();
        response.Should().NotBeEmpty();
        response.Should().HaveCountGreaterThan(0);

        var material = response.First();
        TestContext.WriteLine($"First material details: Name='{material.Name}', Available={material.AvailableQuantityInKg}kg, Price={material.PricePerKg}/kg");

        material.Name.Should().NotBeNullOrEmpty();
        material.Id.Should().BeGreaterThan(0);
        material.AvailableQuantityInKg.Should().BeGreaterThan(0);
        material.PricePerKg.Should().BeGreaterThan(0);

        TestContext.WriteLine($"Material validation successful: ID={material.Id}, Quantity={material.AvailableQuantityInKg}kg");
    }

    [Test]
    [Description("Ensures GetMaterialsAsync returns consistent data across multiple calls")]
    public async Task GetMaterialsAsync_Returns_Consistent_Data()
    {
        TestContext.WriteLine("Testing GetMaterialsAsync for data consistency across multiple calls");

        // Act
        var firstResponse = await _recyclerService.GetMaterialsAsync();
        var secondResponse = await _recyclerService.GetMaterialsAsync();

        TestContext.WriteLine($"First call returned {firstResponse?.Count ?? 0} materials");
        TestContext.WriteLine($"Second call returned {secondResponse?.Count ?? 0} materials");

        // Assert
        firstResponse.Should().NotBeNull();
        secondResponse.Should().NotBeNull();
        firstResponse.Should().HaveCount(secondResponse.Count);

        for (int i = 0; i < firstResponse.Count; i++)
        {
            var firstMaterial = firstResponse[i];
            var secondMaterial = secondResponse[i];

            TestContext.WriteLine($"Comparing material {i}: '{firstMaterial.Name}' vs '{secondMaterial.Name}'");

            firstMaterial.Name.Should().Be(secondMaterial.Name);
            firstMaterial.Id.Should().Be(secondMaterial.Id);
            firstMaterial.PricePerKg.Should().Be(secondMaterial.PricePerKg);
            firstMaterial.AvailableQuantityInKg.Should().Be(secondMaterial.AvailableQuantityInKg);
        }

        TestContext.WriteLine("Data consistency validation successful");
    }

    [Test]
    [Description("Ensures GetMaterialsAsync includes expected materials (sand, copper, steel)")]
    public async Task GetMaterialsAsync_Contains_Expected_Materials()
    {
        TestContext.WriteLine("Testing GetMaterialsAsync for presence of expected materials");

        // Act
        var response = await _recyclerService.GetMaterialsAsync();

        // Assert
        response.Should().NotBeNull();

        var materialNames = response.Select(m => m.Name.ToLowerInvariant()).ToList();
        TestContext.WriteLine($"Available materials: {string.Join(", ", materialNames)}");

        materialNames.Should().Contain("sand");
        materialNames.Should().Contain("copper");
        materialNames.Should().Contain("steel");

        TestContext.WriteLine("Expected materials validation successful");
    }

    [Test]
    [Description("Verifies that CreateOrderAsync processes valid requests correctly")]
    public async Task CreateOrderAsync_With_Valid_Request_Returns_Success()
    {
        TestContext.WriteLine("Testing CreateOrderAsync with valid request");

        // Arrange
        var request = new RecyclerOrderRequest
        {
            CompanyName = "Test Company",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Sand",
                    QuantityInKg = 100
                }
            }
        };

        TestContext.WriteLine($"Creating order for company: '{request.CompanyName}', Items: {request.OrderItems.Count}");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _recyclerService.CreateOrderAsync(request);
        stopwatch.Stop();

        TestContext.WriteLine($"Order creation completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Order ID: {response.data.OrderId}, Account: {response.data.AccountNumber}");

        // Assert
        response.Should().NotBeNull();
        response.data.Should().NotBeNull();
        response.data.OrderId.Should().BeGreaterThan(0);
        response.data.AccountNumber.Should().NotBeNullOrEmpty();
        response.data.OrderItems.Should().NotBeNull();

        TestContext.WriteLine("Order creation validation successful");
    }

    [Test]
    [Description("Tests CreateOrderAsync with multiple items and validates response data")]
    public async Task CreateOrderAsync_With_Multiple_Items_Returns_Correct_Data()
    {
        TestContext.WriteLine("Testing CreateOrderAsync with multiple items");

        // Arrange
        var request = new RecyclerOrderRequest
        {
            CompanyName = "Multi Item Company",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Sand",
                    QuantityInKg = 200
                },
                new RecyclerOrderItem
                {
                    RawMaterialName = "Copper",
                    QuantityInKg = 50
                }
            }
        };

        TestContext.WriteLine($"Creating order for company: '{request.CompanyName}', Items: {request.OrderItems.Count}");

        // Act
        var response = await _recyclerService.CreateOrderAsync(request);

        TestContext.WriteLine($"Order ID: {response.data.OrderId}, Items in response: {response.data.OrderItems.Count}");

        // Assert
        response.Should().NotBeNull();
        response.data.Should().NotBeNull();
        response.data.OrderId.Should().BeGreaterThan(0);
        response.data.AccountNumber.Should().Be("MOCK-RECYCLER-ACC");
        response.data.OrderItems.Should().HaveCount(2);

        var sandItem = response.data.OrderItems.FirstOrDefault(item => item.PricePerKg == 8m);
        var copperItem = response.data.OrderItems.FirstOrDefault(item => item.PricePerKg == 40m);

        sandItem.Should().NotBeNull();
        sandItem!.QuantityInKg.Should().Be(200);

        copperItem.Should().NotBeNull();
        copperItem!.QuantityInKg.Should().Be(50);

        TestContext.WriteLine("Multiple items order validation successful");
    }

    [Test]
    [Description("Verifies that CreateOrderAsync generates unique order IDs for each request")]
    public async Task CreateOrderAsync_Generates_Unique_OrderIds()
    {
        TestContext.WriteLine("Testing CreateOrderAsync for unique order ID generation");

        // Arrange
        var request1 = new RecyclerOrderRequest
        {
            CompanyName = "Company 1",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Sand",
                    QuantityInKg = 100
                }
            }
        };

        var request2 = new RecyclerOrderRequest
        {
            CompanyName = "Company 2",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Copper",
                    QuantityInKg = 50
                }
            }
        };

        // Act
        var response1 = await _recyclerService.CreateOrderAsync(request1);
        var response2 = await _recyclerService.CreateOrderAsync(request2);

        TestContext.WriteLine($"First order ID: {response1.data.OrderId}");
        TestContext.WriteLine($"Second order ID: {response2.data.OrderId}");

        // Assert
        response1.Should().NotBeNull();
        response2.Should().NotBeNull();
        response1.data.OrderId.Should().NotBe(response2.data.OrderId);

        TestContext.WriteLine("Unique order ID validation successful");
    }

    [Test]
    [Description("Tests GetOrdersAsync and validates order summary data")]
    public async Task GetOrdersAsync_Returns_Valid_Order_Summaries()
    {
        TestContext.WriteLine("Testing GetOrdersAsync for valid order summary data");

        // Arrange - First create some orders
        var createRequest = new RecyclerOrderRequest
        {
            CompanyName = "Summary Test Company",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Steel",
                    QuantityInKg = 75
                }
            }
        };

        await _recyclerService.CreateOrderAsync(createRequest);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _recyclerService.GetOrdersAsync();
        stopwatch.Stop();

        TestContext.WriteLine($"API call completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Received {response?.Count ?? 0} order summaries");

        // Assert
        response.Should().NotBeNull();
        response.Should().NotBeEmpty();

        var orderSummary = response.First();
        TestContext.WriteLine($"First order summary: Number='{orderSummary.OrderNumber}', Supplier='{orderSummary.SupplierName}', Status='{orderSummary.Status}'");

        orderSummary.OrderNumber.Should().NotBeNullOrEmpty();
        orderSummary.SupplierName.Should().NotBeNullOrEmpty();
        orderSummary.Status.Should().NotBeNullOrEmpty();
        orderSummary.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        TestContext.WriteLine("Order summaries validation successful");
    }

    [Test]
    [Description("Tests GetOrderByNumberAsync with valid order number")]
    public async Task GetOrderByNumberAsync_With_Valid_OrderNumber_Returns_Details()
    {
        TestContext.WriteLine("Testing GetOrderByNumberAsync with valid order number");

        // Arrange - First create an order
        var createRequest = new RecyclerOrderRequest
        {
            CompanyName = "Detail Test Company",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Sand",
                    QuantityInKg = 150
                }
            }
        };

        await _recyclerService.CreateOrderAsync(createRequest);

        // Get the order number from orders list
        var orders = await _recyclerService.GetOrdersAsync();
        var orderNumber = orders.First().OrderNumber;

        TestContext.WriteLine($"Retrieving details for order: {orderNumber}");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _recyclerService.GetOrderByNumberAsync(orderNumber);
        stopwatch.Stop();

        TestContext.WriteLine($"API call completed in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Order details: Number='{response.OrderNumber}', Items={response.Items.Count}");

        // Assert
        response.Should().NotBeNull();
        response.OrderNumber.Should().Be(orderNumber);
        response.SupplierName.Should().NotBeNullOrEmpty();
        response.Status.Should().NotBeNullOrEmpty();
        response.Items.Should().NotBeNull();
        response.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        TestContext.WriteLine("Order details validation successful");
    }

    [Test]
    [Description("Tests GetOrderByNumberAsync with invalid order number throws DataNotFoundException")]
    public async Task GetOrderByNumberAsync_With_Invalid_OrderNumber_Throws_DataNotFoundException()
    {
        TestContext.WriteLine("Testing GetOrderByNumberAsync with invalid order number");

        // Arrange
        var invalidOrderNumber = "INVALID-ORDER-12345";

        TestContext.WriteLine($"Attempting to retrieve non-existent order: {invalidOrderNumber}");

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _recyclerService.GetOrderByNumberAsync(invalidOrderNumber))
            .Should().ThrowAsync<DataNotFoundException>();

        exception.Which.Message.Should().Contain(invalidOrderNumber);

        TestContext.WriteLine("Invalid order number validation successful");
    }

    [Test]
    [Description("Ensures CreateOrderAsync throws InsufficientStockException for insufficient stock scenarios")]
    public async Task CreateOrderAsync_With_Insufficient_Stock_Throws_InsufficientStockException()
    {
        TestContext.WriteLine("Testing CreateOrderAsync with insufficient stock scenario");

        // Arrange
        var request = new RecyclerOrderRequest
        {
            CompanyName = "INSUFFICIENT_STOCK_COMPANY",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Sand",
                    QuantityInKg = 100
                }
            }
        };

        TestContext.WriteLine($"Creating order for company: '{request.CompanyName}' (insufficient stock scenario)");

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _recyclerService.CreateOrderAsync(request))
            .Should().ThrowAsync<InsufficientStockException>();

        exception.Which.Message.Should().Contain("recycled materials");
        exception.Which.Message.Should().Contain("1");
        exception.Which.Message.Should().Contain("0");

        TestContext.WriteLine("Insufficient stock exception validation successful");
    }

    [Test]
    [Description("Ensures CreateOrderAsync throws RecyclerServiceException on network error")]
    public async Task CreateOrderAsync_With_Network_Error_Throws_RecyclerServiceException()
    {
        TestContext.WriteLine("Testing CreateOrderAsync with network error scenario");

        // Arrange
        var request = new RecyclerOrderRequest
        {
            CompanyName = "NETWORK_ERROR_COMPANY",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Copper",
                    QuantityInKg = 50
                }
            }
        };

        TestContext.WriteLine($"Creating order for company: '{request.CompanyName}' (network error scenario)");

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _recyclerService.CreateOrderAsync(request))
            .Should().ThrowAsync<RecyclerServiceException>();

        exception.Which.Message.Should().Contain("Recycler service unavailable for order creation");
        exception.Which.InnerException.Should().BeOfType<HttpRequestException>();

        TestContext.WriteLine("Network error exception validation successful");
    }

    [Test]
    [Description("Ensures CreateOrderAsync throws RecyclerServiceException on timeout")]
    public async Task CreateOrderAsync_With_Timeout_Throws_RecyclerServiceException()
    {
        TestContext.WriteLine("Testing CreateOrderAsync with timeout scenario");

        // Arrange
        var request = new RecyclerOrderRequest
        {
            CompanyName = "TIMEOUT_COMPANY",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Steel",
                    QuantityInKg = 25
                }
            }
        };

        TestContext.WriteLine($"Creating order for company: '{request.CompanyName}' (timeout scenario)");

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _recyclerService.CreateOrderAsync(request))
            .Should().ThrowAsync<RecyclerServiceException>();

        exception.Which.Message.Should().Contain("Recycler service timeout during order creation");
        exception.Which.InnerException.Should().BeOfType<TaskCanceledException>();

        TestContext.WriteLine("Timeout exception validation successful");
    }

    [Test]
    [Description("Ensures GetOrderByNumberAsync throws RecyclerServiceException on network error")]
    public async Task GetOrderByNumberAsync_With_Network_Error_Throws_RecyclerServiceException()
    {
        TestContext.WriteLine("Testing GetOrderByNumberAsync with network error scenario");

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _recyclerService.GetOrderByNumberAsync("NETWORK_ERROR_ORDER"))
            .Should().ThrowAsync<RecyclerServiceException>();

        exception.Which.Message.Should().Contain("Recycler service unavailable for order NETWORK_ERROR_ORDER retrieval");
        exception.Which.InnerException.Should().BeOfType<HttpRequestException>();

        TestContext.WriteLine("Network error exception validation successful");
    }

    [Test]
    [Description("Ensures GetOrderByNumberAsync throws RecyclerServiceException on timeout")]
    public async Task GetOrderByNumberAsync_With_Timeout_Throws_RecyclerServiceException()
    {
        TestContext.WriteLine("Testing GetOrderByNumberAsync with timeout scenario");

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _recyclerService.GetOrderByNumberAsync("TIMEOUT_ORDER"))
            .Should().ThrowAsync<RecyclerServiceException>();

        exception.Which.Message.Should().Contain("Recycler service timeout during order TIMEOUT_ORDER retrieval");
        exception.Which.InnerException.Should().BeOfType<TaskCanceledException>();

        TestContext.WriteLine("Timeout exception validation successful");
    }

    [Test]
    [Description("Ensures GetOrderByNumberAsync throws DataNotFoundException for specific test order")]
    public async Task GetOrderByNumberAsync_With_NotFound_Order_Throws_DataNotFoundException()
    {
        TestContext.WriteLine("Testing GetOrderByNumberAsync with not found scenario");

        // Act & Assert
        var exception = await FluentActions
            .Invoking(() => _recyclerService.GetOrderByNumberAsync("NOT_FOUND_ORDER"))
            .Should().ThrowAsync<DataNotFoundException>();

        exception.Which.Message.Should().Contain("Recycler order NOT_FOUND_ORDER");

        TestContext.WriteLine("Not found exception validation successful");
    }

    [Test]
    [Description("Tests thread safety of RecyclerService methods with concurrent operations")]
    public async Task RecyclerService_Methods_Are_Thread_Safe()
    {
        TestContext.WriteLine("Testing RecyclerService methods for thread safety");

        // Arrange
        var tasks = new List<Task>();
        var taskNames = new List<string>();

        // Act - Run multiple operations concurrently
        tasks.Add(_recyclerService.GetMaterialsAsync());
        taskNames.Add("GetMaterialsAsync");

        tasks.Add(_recyclerService.GetOrdersAsync());
        taskNames.Add("GetOrdersAsync");

        tasks.Add(_recyclerService.CreateOrderAsync(new RecyclerOrderRequest
        {
            CompanyName = "Concurrent Test 1",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem { RawMaterialName = "Sand", QuantityInKg = 10 }
            }
        }));
        taskNames.Add("CreateOrderAsync");

        tasks.Add(_recyclerService.CreateOrderAsync(new RecyclerOrderRequest
        {
            CompanyName = "Concurrent Test 2",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem { RawMaterialName = "Copper", QuantityInKg = 5 }
            }
        }));
        taskNames.Add("CreateOrderAsync2");

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
    public async Task RecyclerService_Multiple_Consecutive_Calls_Work_Correctly()
    {
        TestContext.WriteLine("Testing RecyclerService with multiple consecutive calls");

        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act & Assert - Multiple consecutive calls should work without issues
        for (int i = 0; i < 5; i++)
        {
            TestContext.WriteLine($"--- Iteration {i + 1} ---");

            var iterationStopwatch = System.Diagnostics.Stopwatch.StartNew();

            var materialsResponse = await _recyclerService.GetMaterialsAsync();
            materialsResponse.Should().NotBeNull();
            materialsResponse.Should().NotBeEmpty();
            TestContext.WriteLine($"GetMaterialsAsync: {materialsResponse.Count} materials");

            var ordersResponse = await _recyclerService.GetOrdersAsync();
            ordersResponse.Should().NotBeNull();
            TestContext.WriteLine($"GetOrdersAsync: {ordersResponse.Count} orders");

            var createRequest = new RecyclerOrderRequest
            {
                CompanyName = $"Consecutive Test {i + 1}",
                OrderItems = new List<RecyclerOrderItem>
                {
                    new RecyclerOrderItem
                    {
                        RawMaterialName = "Sand",
                        QuantityInKg = 10 + i
                    }
                }
            };

            var createResponse = await _recyclerService.CreateOrderAsync(createRequest);
            createResponse.Should().NotBeNull();
            createResponse.data.OrderId.Should().BeGreaterThan(0);
            TestContext.WriteLine($"CreateOrderAsync: Order ID {createResponse.data.OrderId}");

            iterationStopwatch.Stop();
            TestContext.WriteLine($"Iteration {i + 1} completed in {iterationStopwatch.ElapsedMilliseconds}ms");
        }

        totalStopwatch.Stop();
        TestContext.WriteLine($"All 5 iterations completed in {totalStopwatch.ElapsedMilliseconds}ms (avg: {totalStopwatch.ElapsedMilliseconds / 5}ms per iteration)");

        TestContext.WriteLine("Multiple consecutive calls validation successful");
    }

    [Test]
    [Description("Tests complete recycler workflow from materials discovery to order creation and retrieval")]
    public async Task RecyclerService_Workflow_Complete_Order_Cycle()
    {
        TestContext.WriteLine("Testing complete RecyclerService order workflow");

        var workflowStopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Step 1: Get available materials
        TestContext.WriteLine("Step 1: Getting available materials...");
        var materialsResponse = await _recyclerService.GetMaterialsAsync();
        materialsResponse.Should().NotBeNull();
        materialsResponse.Should().NotBeEmpty();
        TestContext.WriteLine($"Found {materialsResponse.Count} available materials");

        // Step 2: Create an order
        TestContext.WriteLine("Step 2: Creating an order...");
        var orderRequest = new RecyclerOrderRequest
        {
            CompanyName = "Workflow Test Company",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = materialsResponse.First().Name,
                    QuantityInKg = 100
                }
            }
        };

        var orderResponse = await _recyclerService.CreateOrderAsync(orderRequest);
        orderResponse.Should().NotBeNull();
        orderResponse.data.OrderId.Should().BeGreaterThan(0);
        TestContext.WriteLine($"Order creation successful - Order ID: {orderResponse.data.OrderId}, Account: {orderResponse.data.AccountNumber}");

        // Step 3: Get orders list
        TestContext.WriteLine("Step 3: Getting orders list...");
        var ordersResponse = await _recyclerService.GetOrdersAsync();
        ordersResponse.Should().NotBeNull();
        ordersResponse.Should().NotBeEmpty();
        TestContext.WriteLine($"Found {ordersResponse.Count} orders");

        // Step 4: Get order details
        TestContext.WriteLine("Step 4: Getting order details...");
        var orderNumber = ordersResponse.Last().OrderNumber; // Get the latest order
        var orderDetailResponse = await _recyclerService.GetOrderByNumberAsync(orderNumber);
        orderDetailResponse.Should().NotBeNull();
        orderDetailResponse.OrderNumber.Should().Be(orderNumber);
        TestContext.WriteLine($"Order details retrieved - Number: {orderDetailResponse.OrderNumber}, Items: {orderDetailResponse.Items.Count}");

        workflowStopwatch.Stop();
        TestContext.WriteLine($"Complete workflow finished in {workflowStopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine("Complete order workflow validation successful");
    }

    [Test]
    [Description("Verifies RecyclerService exception handling does not affect subsequent calls")]
    public async Task RecyclerService_Exception_Handling_Does_Not_Affect_Subsequent_Calls()
    {
        TestContext.WriteLine("Testing RecyclerService exception handling isolation");

        // Arrange & Act - First call that throws exception
        try
        {
            await _recyclerService.CreateOrderAsync(new RecyclerOrderRequest
            {
                CompanyName = "NETWORK_ERROR_COMPANY",
                OrderItems = new List<RecyclerOrderItem>
                {
                    new RecyclerOrderItem { RawMaterialName = "Sand", QuantityInKg = 100 }
                }
            });
        }
        catch (RecyclerServiceException)
        {
            TestContext.WriteLine("Expected exception caught, continuing with test");
        }

        // Act & Assert - Subsequent calls should work normally
        var materialsResponse = await _recyclerService.GetMaterialsAsync();
        materialsResponse.Should().NotBeNull();
        materialsResponse.Should().NotBeEmpty();

        var ordersResponse = await _recyclerService.GetOrdersAsync();
        ordersResponse.Should().NotBeNull();

        var successfulOrder = await _recyclerService.CreateOrderAsync(new RecyclerOrderRequest
        {
            CompanyName = "Recovery Test Company",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem { RawMaterialName = "Copper", QuantityInKg = 25 }
            }
        });
        successfulOrder.Should().NotBeNull();
        successfulOrder.data.OrderId.Should().BeGreaterThan(0);

        TestContext.WriteLine("Exception handling isolation validation successful");
    }

    [Test]
    [Description("Ensures all RecyclerService methods return non-null results")]
    public async Task RecyclerService_All_Methods_Return_NonNull_Results()
    {
        TestContext.WriteLine("Testing RecyclerService methods for non-null results");

        // Act & Assert
        var materials = await _recyclerService.GetMaterialsAsync();
        materials.Should().NotBeNull("GetMaterialsAsync should never return null");

        var orders = await _recyclerService.GetOrdersAsync();
        orders.Should().NotBeNull("GetOrdersAsync should never return null");

        var orderCreation = await _recyclerService.CreateOrderAsync(new RecyclerOrderRequest
        {
            CompanyName = "NonNull Test Company",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem { RawMaterialName = "Steel", QuantityInKg = 15 }
            }
        });
        orderCreation.Should().NotBeNull("CreateOrderAsync should never return null");

        TestContext.WriteLine("Non-null results validation successful");
    }

    [Test]
    [Description("Verifies that GetMaterialsAsync includes materials with expected pricing")]
    public async Task GetMaterialsAsync_Materials_Have_Expected_Pricing()
    {
        TestContext.WriteLine("Testing GetMaterialsAsync for expected material pricing");

        // Act
        var response = await _recyclerService.GetMaterialsAsync();

        // Assert
        response.Should().NotBeNull();
        response.Should().NotBeEmpty();

        var sandMaterial = response.FirstOrDefault(m => m.Name.Equals("Sand", StringComparison.OrdinalIgnoreCase));
        var copperMaterial = response.FirstOrDefault(m => m.Name.Equals("Copper", StringComparison.OrdinalIgnoreCase));
        var steelMaterial = response.FirstOrDefault(m => m.Name.Equals("Steel", StringComparison.OrdinalIgnoreCase));

        sandMaterial.Should().NotBeNull("Sand should be available");
        sandMaterial!.PricePerKg.Should().Be(8f);
        sandMaterial.AvailableQuantityInKg.Should().Be(10000f);

        copperMaterial.Should().NotBeNull("Copper should be available");
        copperMaterial!.PricePerKg.Should().Be(40f);
        copperMaterial.AvailableQuantityInKg.Should().Be(5000f);

        steelMaterial.Should().NotBeNull("Steel should be available");
        steelMaterial!.PricePerKg.Should().Be(25f);
        steelMaterial.AvailableQuantityInKg.Should().Be(3000f);

        TestContext.WriteLine($"Sand: {sandMaterial.PricePerKg}/kg, Copper: {copperMaterial.PricePerKg}/kg, Steel: {steelMaterial.PricePerKg}/kg");
        TestContext.WriteLine("Material pricing validation successful");
    }

    [Test]
    [Description("Tests CreateOrderAsync pricing calculation accuracy")]
    public async Task CreateOrderAsync_Pricing_Calculation_Is_Accurate()
    {
        TestContext.WriteLine("Testing CreateOrderAsync pricing calculation accuracy");

        // Arrange
        var sandRequest = new RecyclerOrderRequest
        {
            CompanyName = "Sand Pricing Test",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Sand",
                    QuantityInKg = 100
                }
            }
        };

        var copperRequest = new RecyclerOrderRequest
        {
            CompanyName = "Copper Pricing Test",
            OrderItems = new List<RecyclerOrderItem>
            {
                new RecyclerOrderItem
                {
                    RawMaterialName = "Copper",
                    QuantityInKg = 50
                }
            }
        };

        // Act
        var sandResponse = await _recyclerService.CreateOrderAsync(sandRequest);
        var copperResponse = await _recyclerService.CreateOrderAsync(copperRequest);

        // Assert
        sandResponse.Should().NotBeNull();
        copperResponse.Should().NotBeNull();

        // Sand: 100kg * 8 = 800, Copper: 50kg * 40 = 2000 (based on MockRecyclerService pricing)
        var sandItem = sandResponse.data.OrderItems.FirstOrDefault();
        var copperItem = copperResponse.data.OrderItems.FirstOrDefault();

        sandItem.Should().NotBeNull();
        sandItem!.PricePerKg.Should().Be(8m);
        sandItem.QuantityInKg.Should().Be(100);

        copperItem.Should().NotBeNull();
        copperItem!.PricePerKg.Should().Be(40m);
        copperItem.QuantityInKg.Should().Be(50);

        TestContext.WriteLine($"Sand pricing: {sandItem.QuantityInKg}kg * {sandItem.PricePerKg}/kg");
        TestContext.WriteLine($"Copper pricing: {copperItem.QuantityInKg}kg * {copperItem.PricePerKg}/kg");
        TestContext.WriteLine("Pricing calculation validation successful");
    }
}