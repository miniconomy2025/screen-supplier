using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
public sealed class PurchaseOrderServiceStepDefinitions
{
    private readonly DbContextOptions<ScreenContext> _dbOptions;
    private ScreenContext _context;
    private Mock<SimulationTimeService> _mockTimeService;
    private SimulationTimeProvider _timeProvider;
    private PurchaseOrderService _purchaseOrderService;

    private PurchaseOrder? _result;
    private PurchaseOrder? _foundOrder;
    private List<PurchaseOrder>? _foundOrders;
    private bool _operationResult;
    private DateTime _simulationTime;

    // Store created purchase orders by their test ID for later reference
    private Dictionary<int, PurchaseOrder> _createdOrders = new Dictionary<int, PurchaseOrder>();

    public PurchaseOrderServiceStepDefinitions()
    {
        // Use in-memory database for testing
        _dbOptions = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        SetupServices();
    }

    private void SetupServices()
    {
        _context = new ScreenContext(_dbOptions);

        // Create a mock for SimulationTimeService and use it to create a real SimulationTimeProvider
        _mockTimeService = new Mock<SimulationTimeService>(
            Mock.Of<ILogger<SimulationTimeService>>(),
            Mock.Of<IServiceProvider>());

        _simulationTime = DateTime.UtcNow;
        _mockTimeService.Setup(x => x.GetSimulationDateTime()).Returns(_simulationTime);
        _mockTimeService.Setup(x => x.IsSimulationRunning()).Returns(true); // Default to running for tests

        _timeProvider = new SimulationTimeProvider(_mockTimeService.Object);
        _purchaseOrderService = new PurchaseOrderService(_context, _timeProvider);

        // Ensure database is created and clean
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        // Clear the created orders dictionary
        _createdOrders.Clear();
    }

    [Given(@"the database has order status ""(.*)""")]
    public async Task GivenTheDatabaseHasOrderStatus(string statusName)
    {
        var orderStatus = new OrderStatus
        {
            Status = statusName
        };

        _context.OrderStatuses.Add(orderStatus);
        await _context.SaveChangesAsync();
    }

    [Given(@"the database does not have order status ""(.*)""")]
    public void GivenTheDatabaseDoesNotHaveOrderStatus(string statusName)
    {
        // Ensure the status doesn't exist - this is the default state
        var existingStatus = _context.OrderStatuses.FirstOrDefault(os => os.Status == statusName);
        if (existingStatus != null)
        {
            _context.OrderStatuses.Remove(existingStatus);
            _context.SaveChanges();
        }
    }

    [Given(@"there is a purchase order with shipment ID {int}")]
    public async Task GivenThereIsAPurchaseOrderWithShipmentID(int shipmentId)
    {
        await EnsureRequiredStatusExists();

        var order = new PurchaseOrder
        {
            OrderID = 100,
            ShipmentID = shipmentId,
            Quantity = 50,
            UnitPrice = 100,
            BankAccountNumber = "test-account",
            Origin = "test-supplier",
            OrderDate = _simulationTime,
            OrderStatusId = 1
        };

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();
    }

    [Given(@"there is no purchase order with shipment ID {int}")]
    public void GivenThereIsNoPurchaseOrderWithShipmentID(int shipmentId)
    {
        // Ensure no order exists with this shipment ID - this is the default state
        var existingOrder = _context.PurchaseOrders.FirstOrDefault(po => po.ShipmentID == shipmentId);
        if (existingOrder != null)
        {
            _context.PurchaseOrders.Remove(existingOrder);
            _context.SaveChanges();
        }
    }

    [Given(@"there is a purchase order with ID {int}")]
    public async Task GivenThereIsAPurchaseOrderWithID(int testOrderId)
    {
        await EnsureRequiredStatusExists();

        var order = new PurchaseOrder
        {
            OrderID = 100 + testOrderId,
            Quantity = 100,
            UnitPrice = 50,
            BankAccountNumber = "test-account",
            Origin = "test-supplier",
            OrderDate = _simulationTime,
            OrderStatusId = 1
        };

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();

        // Store the created order with its test ID for later reference
        _createdOrders[testOrderId] = order;
    }

    [Given(@"there is no purchase order with ID {int}")]
    public void GivenThereIsNoPurchaseOrderWithID(int testOrderId)
    {
        // Remove any stored reference and clear database
        if (_createdOrders.ContainsKey(testOrderId))
        {
            _createdOrders.Remove(testOrderId);
        }

        // For in-memory database, we ensure clean state by removing all orders
        var allOrders = _context.PurchaseOrders.ToList();
        if (allOrders.Any())
        {
            _context.PurchaseOrders.RemoveRange(allOrders);
            _context.SaveChanges();
        }
    }

    [Given(@"there is a purchase order with ID {int} and quantity {int}")]
    public async Task GivenThereIsAPurchaseOrderWithIDAndQuantity(int testOrderId, int quantity)
    {
        await EnsureRequiredStatusExists();

        var order = new PurchaseOrder
        {
            OrderID = 100 + testOrderId,
            Quantity = quantity,
            QuantityDelivered = 0,
            UnitPrice = 50,
            BankAccountNumber = "test-account",
            Origin = "test-supplier",
            OrderDate = _simulationTime,
            OrderStatusId = 1
        };

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();

        // Store the created order with its test ID for later reference
        _createdOrders[testOrderId] = order;
    }

    [Given(@"there are purchase orders with various statuses")]
    public async Task GivenThereArePurchaseOrdersWithVariousStatuses()
    {
        // Create required statuses
        var statuses = new[]
        {
            new OrderStatus { Status = Status.RequiresPaymentToSupplier },
            new OrderStatus { Status = Status.RequiresDelivery },
            new OrderStatus { Status = Status.Delivered },
            new OrderStatus { Status = Status.WaitingForDelivery }
        };

        _context.OrderStatuses.AddRange(statuses);
        await _context.SaveChangesAsync();

        // Create orders with various statuses
        var orders = new[]
        {
            new PurchaseOrder
            {
                OrderID = 101,
                Quantity = 50,
                UnitPrice = 100,
                BankAccountNumber = "test-account-1",
                Origin = "supplier-1",
                OrderDate = _simulationTime.AddDays(-2),
                OrderStatusId = statuses[0].Id // RequiresPaymentToSupplier
            },
            new PurchaseOrder
            {
                OrderID = 102,
                Quantity = 75,
                UnitPrice = 150,
                BankAccountNumber = "test-account-2",
                Origin = "supplier-2",
                OrderDate = _simulationTime.AddDays(-1),
                OrderStatusId = statuses[1].Id // RequiresDelivery
            },
            new PurchaseOrder
            {
                OrderID = 103,
                Quantity = 100,
                UnitPrice = 200,
                BankAccountNumber = "test-account-3",
                Origin = "supplier-3",
                OrderDate = _simulationTime,
                OrderStatusId = statuses[2].Id // Delivered
            },
            new PurchaseOrder
            {
                OrderID = 104,
                Quantity = 25,
                UnitPrice = 75,
                BankAccountNumber = "test-account-4",
                Origin = "supplier-4",
                OrderDate = _simulationTime.AddHours(-6),
                OrderStatusId = statuses[3].Id // WaitingForDelivery
            }
        };

        _context.PurchaseOrders.AddRange(orders);
        await _context.SaveChangesAsync();
    }

    [Given(@"there are multiple purchase orders with different dates")]
    public async Task GivenThereAreMultiplePurchaseOrdersWithDifferentDates()
    {
        await EnsureRequiredStatusExists();

        // Create orders with different dates (more than 100 to test limit)
        var orders = new List<PurchaseOrder>();
        for (int i = 1; i <= 150; i++)
        {
            orders.Add(new PurchaseOrder
            {
                OrderID = 1000 + i,
                Quantity = 10 + i,
                UnitPrice = 50 + i,
                BankAccountNumber = $"test-account-{i}",
                Origin = $"supplier-{i}",
                OrderDate = _simulationTime.AddDays(-i), // Different dates
                OrderStatusId = 1
            });
        }

        _context.PurchaseOrders.AddRange(orders);
        await _context.SaveChangesAsync();
    }

    [When(@"I create a purchase order with order ID (.*), quantity (.*), unit price (.*), bank account ""(.*)"", origin ""(.*)"", material ID (.*), and equipment order (.*)")]
    public async Task WhenICreateAPurchaseOrder(int orderId, int quantity, int unitPrice, string bankAccount, string origin, string materialIdStr, bool isEquipmentOrder)
    {
        int? materialId = materialIdStr == "null" ? null : int.Parse(materialIdStr);

        _result = await _purchaseOrderService.CreatePurchaseOrderAsync(
            orderId, quantity, unitPrice, bankAccount, origin, materialId, isEquipmentOrder);
    }

    [When(@"I find purchase order by shipment ID (.*)")]
    public async Task WhenIFindPurchaseOrderByShipmentID(int shipmentId)
    {
        _foundOrder = await _purchaseOrderService.FindPurchaseOrderByShipmentIdAsync(shipmentId);
    }

    [When(@"I update shipment ID to (.*) for purchase order (.*)")]
    public async Task WhenIUpdateShipmentIDForPurchaseOrder(int shipmentId, int testOrderId)
    {
        // Get the actual database ID from our stored orders
        var actualPurchaseOrderId = _createdOrders.ContainsKey(testOrderId)
            ? _createdOrders[testOrderId].Id
            : testOrderId; // fallback to test ID if not found

        _operationResult = await _purchaseOrderService.UpdateShipmentIdAsync(actualPurchaseOrderId, shipmentId);
    }

    [When(@"I update delivery quantity by (.*) for purchase order (.*)")]
    public async Task WhenIUpdateDeliveryQuantityForPurchaseOrder(int deliveryQuantity, int testOrderId)
    {
        // Get the actual database ID from our stored orders
        var actualPurchaseOrderId = _createdOrders.ContainsKey(testOrderId)
            ? _createdOrders[testOrderId].Id
            : testOrderId; // fallback to test ID if not found

        _operationResult = await _purchaseOrderService.UpdateDeliveryQuantityAsync(actualPurchaseOrderId, deliveryQuantity);
    }

    [When(@"I update status to ""(.*)"" for purchase order (.*)")]
    public async Task WhenIUpdateStatusForPurchaseOrder(string statusName, int testOrderId)
    {
        // Get the actual database ID from our stored orders
        var actualPurchaseOrderId = _createdOrders.ContainsKey(testOrderId)
            ? _createdOrders[testOrderId].Id
            : testOrderId; // fallback to test ID if not found

        _operationResult = await _purchaseOrderService.UpdateStatusAsync(actualPurchaseOrderId, statusName);
    }

    [When(@"I update shipping details with bank account ""(.*)"" and shipping price (.*) for purchase order (.*)")]
    public async Task WhenIUpdateShippingDetailsForPurchaseOrder(string bankAccount, int shippingPrice, int testOrderId)
    {
        // Get the actual database ID from our stored orders
        var actualPurchaseOrderId = _createdOrders.ContainsKey(testOrderId)
            ? _createdOrders[testOrderId].Id
            : testOrderId; // fallback to test ID if not found

        _operationResult = await _purchaseOrderService.UpdateOrderShippingDetailsAsync(actualPurchaseOrderId, bankAccount, shippingPrice);
    }

    [When(@"I get purchase order by ID (.*)")]
    public async Task WhenIGetPurchaseOrderByID(int testOrderId)
    {
        // Get the actual database ID from our stored orders
        var actualPurchaseOrderId = _createdOrders.ContainsKey(testOrderId)
            ? _createdOrders[testOrderId].Id
            : testOrderId; // fallback to test ID if not found

        _foundOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(actualPurchaseOrderId);
    }

    [When(@"I get active purchase orders")]
    public async Task WhenIGetActivePurchaseOrders()
    {
        _foundOrders = await _purchaseOrderService.GetActivePurchaseOrdersAsync();
    }

    [When(@"I get all orders")]
    public async Task WhenIGetAllOrders()
    {
        _foundOrders = await _purchaseOrderService.GetOrdersAsync();
    }

    [When(@"I get past orders for a specific date")]
    public async Task WhenIGetPastOrdersForASpecificDate()
    {
        _foundOrders = await _purchaseOrderService.GetPastOrdersAsync(_simulationTime.AddDays(-30));
    }

    [Then(@"the purchase order should be created successfully")]
    public void ThenThePurchaseOrderShouldBeCreatedSuccessfully()
    {
        _result.Should().NotBeNull();
        _result!.Id.Should().BeGreaterThan(0);
    }

    [Then(@"the purchase order should not be created")]
    public void ThenThePurchaseOrderShouldNotBeCreated()
    {
        _result.Should().BeNull();
    }

    [Then(@"the result should be null")]
    public void ThenTheResultShouldBeNull()
    {
        _result.Should().BeNull();
    }

    [Then(@"the order should have order ID (.*)")]
    public void ThenTheOrderShouldHaveOrderID(int expectedOrderId)
    {
        _result!.OrderID.Should().Be(expectedOrderId);
    }

    [Then(@"the order should have quantity (.*)")]
    public void ThenTheOrderShouldHaveQuantity(int expectedQuantity)
    {
        _result!.Quantity.Should().Be(expectedQuantity);
    }

    [Then(@"the order should have unit price (.*)")]
    public void ThenTheOrderShouldHaveUnitPrice(int expectedUnitPrice)
    {
        _result!.UnitPrice.Should().Be(expectedUnitPrice);
    }

    [Then(@"the order should have bank account ""(.*)""")]
    public void ThenTheOrderShouldHaveBankAccount(string expectedBankAccount)
    {
        _result!.BankAccountNumber.Should().Be(expectedBankAccount);
    }

    [Then(@"the order should have origin ""(.*)""")]
    public void ThenTheOrderShouldHaveOrigin(string expectedOrigin)
    {
        _result!.Origin.Should().Be(expectedOrigin);
    }

    [Then(@"the order should have material ID (.*)")]
    public void ThenTheOrderShouldHaveMaterialID(int expectedMaterialId)
    {
        _result!.RawMaterialId.Should().Be(expectedMaterialId);
    }

    [Then(@"the order should have no material ID")]
    public void ThenTheOrderShouldHaveNoMaterialID()
    {
        _result!.RawMaterialId.Should().BeNull();
    }

    [Then(@"the order should be an equipment order")]
    public void ThenTheOrderShouldBeAnEquipmentOrder()
    {
        _result!.EquipmentOrder.Should().BeTrue();
    }

    [Then(@"the order should not be an equipment order")]
    public void ThenTheOrderShouldNotBeAnEquipmentOrder()
    {
        _result!.EquipmentOrder.Should().BeFalse();
    }

    [Then(@"the order should have status (.*)")]
    public void ThenTheOrderShouldHaveStatusParameterized(string expectedStatus)
    {
        // This handles scenario outline parameters like <expectedStatus>
        PurchaseOrder dbOrder;

        if (_result != null)
        {
            // This is for creation scenarios - we have a result from creating an order
            dbOrder = _context.PurchaseOrders
                .Include(po => po.OrderStatus)
                .First(po => po.Id == _result.Id);
        }
        else
        {
            // This is for update scenarios - we need to find the order that was updated
            // Get the most recently updated order with the expected status
            dbOrder = _context.PurchaseOrders
                .Include(po => po.OrderStatus)
                .Where(po => po.OrderStatus.Status == expectedStatus)
                .OrderByDescending(po => po.Id)
                .First();
        }

        dbOrder.OrderStatus.Status.Should().Be(expectedStatus);
    }

    [Then(@"the order date should be ""(.*)""")]
    public void ThenTheOrderDateShouldBe(string expectedDateString)
    {
        var expectedDate = DateTime.Parse(expectedDateString);
        _result!.OrderDate.Should().Be(expectedDate);
    }

    [Then(@"the purchase order should be found")]
    public void ThenThePurchaseOrderShouldBeFound()
    {
        _foundOrder.Should().NotBeNull();
    }

    [Then(@"no purchase order should be found")]
    public void ThenNoPurchaseOrderShouldBeFound()
    {
        _foundOrder.Should().BeNull();
    }

    [Then(@"the order should have shipment ID (.*)")]
    public void ThenTheOrderShouldHaveShipmentID(int expectedShipmentId)
    {
        if (_foundOrder != null)
        {
            _foundOrder.ShipmentID.Should().Be(expectedShipmentId);
        }
        else
        {
            // Check in database for updated order
            var updatedOrder = _context.PurchaseOrders.FirstOrDefault(po => po.ShipmentID == expectedShipmentId);
            updatedOrder.Should().NotBeNull();
        }
    }

    [Then(@"the (shipment ID update|delivery quantity update|status update|shipping details update) should be successful")]
    public void ThenTheOperationShouldBeSuccessful(string operation)
    {
        _operationResult.Should().BeTrue();
    }

    [Then(@"the (shipment ID update|delivery quantity update|status update|shipping details update) should fail")]
    public void ThenTheOperationShouldFail(string operation)
    {
        _operationResult.Should().BeFalse();
    }

    [Then(@"the order should have delivered quantity (.*)")]
    public void ThenTheOrderShouldHaveDeliveredQuantity(int expectedDelivered)
    {
        var updatedOrder = _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .FirstOrDefault(po => po.QuantityDelivered == expectedDelivered);

        updatedOrder.Should().NotBeNull();
        updatedOrder!.QuantityDelivered.Should().Be(expectedDelivered);
    }

    [Then(@"the order should have shipper bank account ""(.*)""")]
    public void ThenTheOrderShouldHaveShipperBankAccount(string expectedBankAccount)
    {
        var updatedOrder = _context.PurchaseOrders
            .FirstOrDefault(po => po.ShipperBankAccout == expectedBankAccount);

        updatedOrder.Should().NotBeNull();
        updatedOrder!.ShipperBankAccout.Should().Be(expectedBankAccount);
    }

    [Then(@"the order should have shipping price (.*)")]
    public void ThenTheOrderShouldHaveShippingPrice(int expectedShippingPrice)
    {
        var updatedOrder = _context.PurchaseOrders
            .FirstOrDefault(po => po.OrderShippingPrice == expectedShippingPrice);

        updatedOrder.Should().NotBeNull();
        updatedOrder!.OrderShippingPrice.Should().Be(expectedShippingPrice);
    }

    [Then(@"the purchase order should be retrieved successfully")]
    public void ThenThePurchaseOrderShouldBeRetrievedSuccessfully()
    {
        _foundOrder.Should().NotBeNull();
    }

    [Then(@"no purchase order should be retrieved")]
    public void ThenNoPurchaseOrderShouldBeRetrieved()
    {
        _foundOrder.Should().BeNull();
    }

    [Then(@"only non-delivered orders should be returned")]
    public void ThenOnlyNonDeliveredOrdersShouldBeReturned()
    {
        _foundOrders.Should().NotBeNull();
        _foundOrders!.Should().NotBeEmpty();
        _foundOrders.Should().NotContain(o => o.OrderStatus.Status == Status.Delivered);
    }

    [Then(@"delivered orders should not be included")]
    public void ThenDeliveredOrdersShouldNotBeIncluded()
    {
        _foundOrders.Should().NotContain(o => o.OrderStatus.Status == Status.Delivered);
    }

    [Then(@"all orders should be returned including delivered ones")]
    public void ThenAllOrdersShouldBeReturnedIncludingDeliveredOnes()
    {
        _foundOrders.Should().NotBeNull();
        _foundOrders!.Should().NotBeEmpty();
        _foundOrders.Should().Contain(o => o.OrderStatus.Status == Status.Delivered);
        _foundOrders.Should().Contain(o => o.OrderStatus.Status != Status.Delivered);
    }

    [Then(@"orders should be returned in descending date order")]
    public void ThenOrdersShouldBeReturnedInDescendingDateOrder()
    {
        _foundOrders.Should().NotBeNull();
        _foundOrders!.Should().NotBeEmpty();

        for (int i = 0; i < _foundOrders.Count - 1; i++)
        {
            _foundOrders[i].OrderDate.Should().BeOnOrAfter(_foundOrders[i + 1].OrderDate);
        }
    }

    [Then(@"no more than (.*) orders should be returned")]
    public void ThenNoMoreThanOrdersShouldBeReturned(int maxCount)
    {
        _foundOrders.Should().NotBeNull();
        _foundOrders!.Count.Should().BeLessOrEqualTo(maxCount);
    }

    [Then(@"the (.*) update should (fail|succeed)")]
    public void ThenTheUpdateShouldFailOrSucceed(string updateType, string result)
    {
        bool expectedResult = result == "succeed";
        _operationResult.Should().Be(expectedResult);
    }

    private async Task EnsureRequiredStatusExists()
    {
        var existingStatus = await _context.OrderStatuses
            .FirstOrDefaultAsync(os => os.Status == Status.RequiresPaymentToSupplier);

        if (existingStatus == null)
        {
            var orderStatus = new OrderStatus
            {
                Status = Status.RequiresPaymentToSupplier
            };

            _context.OrderStatuses.Add(orderStatus);
            await _context.SaveChangesAsync();
        }
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _context?.Dispose();
        SetupServices();
    }
}

// Extension method to convert strings to title case
public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
            }
        }
        return string.Join(" ", words);
    }
}