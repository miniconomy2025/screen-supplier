using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
public sealed class ScreenOrderServiceStepDefinitions
{
    private ScreenContext _context = null!;
    private Mock<ILogger<ScreenOrderService>> _mockLogger = null!;
    private Mock<ProductService> _mockProductService = null!;
    private Mock<SimulationTimeProvider> _mockTimeProvider = null!;
    private ScreenOrderService _screenOrderService = null!;
    private ScreenOrder? _createdOrder;
    private ScreenOrder? _retrievedOrder;
    private List<ScreenOrder>? _retrievedOrders;
    private PaymentConfirmationResponse? _paymentResponse;
    private bool _updateStatusResult;
    private bool _updatePaymentResult;
    private bool _updateQuantityResult;
    private Exception? _caughtException;
    private int _availableStock;

    [BeforeScenario]
    public void SetupMocks()
    {
        var options = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ScreenContext(options);

        // Seed order statuses
        if (!_context.OrderStatuses.Any())
        {
            _context.OrderStatuses.AddRange(
                new OrderStatus { Id = 1, Status = Status.WaitingForPayment },
                new OrderStatus { Id = 2, Status = Status.WaitingForCollection },
                new OrderStatus { Id = 3, Status = Status.Collected }
            );
            _context.SaveChanges();
        }

        _mockLogger = new Mock<ILogger<ScreenOrderService>>();

        var mockMaterialService = new Mock<MaterialService>(_context);
        _mockProductService = new Mock<ProductService>(_context, mockMaterialService.Object);

        var mockLogger2 = new Mock<ILogger<SimulationTimeService>>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockSimulationTimeService = new Mock<SimulationTimeService>(mockLogger2.Object, mockServiceProvider.Object);
        _mockTimeProvider = new Mock<SimulationTimeProvider>(mockSimulationTimeService.Object);
        _mockTimeProvider.Setup(x => x.Now).Returns(DateTime.UtcNow);

        _screenOrderService = new ScreenOrderService(
            _context,
            _mockLogger.Object,
            _mockProductService.Object,
            _mockTimeProvider.Object);
    }

    [Given(@"there is a product with price (.*)")]
    public void GivenThereIsAProductWithPrice(int price)
    {
        var product = new Product
        {
            Id = 1,
            Price = price,
            Quantity = 1000
        };
        _context.Products.Add(product);
        _context.SaveChanges();
        _mockProductService.Setup(x => x.GetProductAsync()).ReturnsAsync(product);
    }

    [Given(@"there are (.*) screens available in stock")]
    public void GivenThereAreScreensAvailableInStock(int availableStock)
    {
        _availableStock = availableStock;
        _mockProductService.Setup(x => x.GetAvailableStockAsync()).ReturnsAsync(availableStock);
    }

    [Given(@"there is no product available")]
    public void GivenThereIsNoProductAvailable()
    {
        _mockProductService.Setup(x => x.GetProductAsync()).ReturnsAsync((Product?)null);
    }

    [Given(@"there is a screen order with id (.*) in status ""(.*)"" with quantity (.*) and unit price (.*)")]
    public void GivenThereIsAScreenOrderWithIdInStatusWithQuantityAndUnitPrice(int orderId, string status, int quantity, int unitPrice)
    {
        var orderStatus = _context.OrderStatuses.First(os => os.Status == status);
        var product = _context.Products.FirstOrDefault();
        if (product == null)
        {
            product = new Product { Id = 1, Price = unitPrice, Quantity = 1000 };
            _context.Products.Add(product);
            _context.SaveChanges();
        }

        var screenOrder = new ScreenOrder
        {
            Id = orderId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            OrderStatusId = orderStatus.Id,
            OrderStatus = orderStatus,
            ProductId = product.Id,
            Product = product,
            OrderDate = DateTime.UtcNow,
            QuantityCollected = 0,
            AmountPaid = 0
        };
        _context.ScreenOrders.Add(screenOrder);
        _context.SaveChanges();
    }

    [Given(@"screen order (.*) has amount paid (.*)")]
    public void GivenScreenOrderHasAmountPaid(int orderId, int amountPaid)
    {
        var order = _context.ScreenOrders.First(o => o.Id == orderId);
        order.AmountPaid = amountPaid;
        _context.SaveChanges();
    }

    [Given(@"there is a bank account")]
    public void GivenThereIsABankAccount()
    {
        var bankAccount = new BankDetails
        {
            AccountNumber = "BANK123",
            EstimatedBalance = 10000
        };
        _context.BankDetails.Add(bankAccount);
        _context.SaveChanges();
    }

    [Given(@"screen order (.*) has quantity collected (.*)")]
    public void GivenScreenOrderHasQuantityCollected(int orderId, int quantityCollected)
    {
        var order = _context.ScreenOrders.First(o => o.Id == orderId);
        order.QuantityCollected = quantityCollected;
        _context.SaveChanges();
    }

    [When(@"I create a screen order with quantity (.*)")]
    public async Task WhenICreateAScreenOrderWithQuantity(int quantity)
    {
        try
        {
            _createdOrder = await _screenOrderService.CreateOrderAsync(quantity);
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }
    }

    [When(@"I process payment confirmation with amount (.*) for order (.*)")]
    public async Task WhenIProcessPaymentConfirmationWithAmountForOrder(decimal amount, int orderId)
    {
        var notification = new TransactionNotification
        {
            Amount = amount,
            Description = orderId.ToString(),
            TransactionNumber = "TXN123",
            Status = "completed"
        };
        _paymentResponse = await _screenOrderService.ProcessPaymentConfirmationAsync(notification, orderId.ToString());
    }

    [When(@"I find screen order by id (.*)")]
    public async Task WhenIFindScreenOrderById(int orderId)
    {
        try
        {
            _retrievedOrder = await _screenOrderService.FindScreenOrderByIdAsync(orderId);
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }
    }

    [When(@"I update status of order (.*) to ""(.*)""")]
    public async Task WhenIUpdateStatusOfOrderTo(int orderId, string newStatus)
    {
        _updateStatusResult = await _screenOrderService.UpdateStatusAsync(orderId, newStatus);
    }

    [When(@"I update payment of order (.*) to (.*)")]
    public async Task WhenIUpdatePaymentOfOrderTo(int orderId, int amountPaid)
    {
        _updatePaymentResult = await _screenOrderService.UpdatePaymentAsync(orderId, amountPaid);
    }

    [When(@"I update quantity collected of order (.*) by (.*)")]
    public async Task WhenIUpdateQuantityCollectedOfOrderBy(int orderId, int quantityCollected)
    {
        _updateQuantityResult = await _screenOrderService.UpdateQuantityCollectedAsync(orderId, quantityCollected);
    }

    [When(@"I get active screen orders")]
    public async Task WhenIGetActiveScreenOrders()
    {
        _retrievedOrders = await _screenOrderService.GetActiveScreenOrdersAsync();
    }

    [When(@"I get orders by status ""(.*)""")]
    public async Task WhenIGetOrdersByStatus(string status)
    {
        _retrievedOrders = await _screenOrderService.GetOrdersByStatusAsync(status);
    }

    [Then(@"the screen order should be created successfully")]
    public void ThenTheScreenOrderShouldBeCreatedSuccessfully()
    {
        _createdOrder.Should().NotBeNull();
        _caughtException.Should().BeNull();
    }

    [Then(@"an exception should be thrown with type ""(.*)""")]
    public void ThenAnExceptionShouldBeThrownWithType(string exceptionType)
    {
        _caughtException.Should().NotBeNull();
        _caughtException!.GetType().Name.Should().Be(exceptionType);
    }

    [Then(@"the created order should have quantity (.*)")]
    public void ThenTheCreatedOrderShouldHaveQuantity(int expectedQuantity)
    {
        _createdOrder.Should().NotBeNull();
        _createdOrder!.Quantity.Should().Be(expectedQuantity);
    }

    [Then(@"the created order should have status ""(.*)""")]
    public void ThenTheCreatedOrderShouldHaveStatus(string expectedStatus)
    {
        _createdOrder.Should().NotBeNull();
        _createdOrder!.OrderStatus.Status.Should().Be(expectedStatus);
    }

    [Then(@"the payment confirmation should be successful")]
    public void ThenThePaymentConfirmationShouldBeSuccessful()
    {
        _paymentResponse.Should().NotBeNull();
        _paymentResponse!.Success.Should().BeTrue();
    }

    [Then(@"the payment confirmation should not be successful")]
    public void ThenThePaymentConfirmationShouldNotBeSuccessful()
    {
        _paymentResponse.Should().NotBeNull();
        _paymentResponse!.Success.Should().BeFalse();
    }

    [Then(@"the payment response should indicate order is fully paid")]
    public void ThenThePaymentResponseShouldIndicateOrderIsFullyPaid()
    {
        _paymentResponse.Should().NotBeNull();
        _paymentResponse!.IsFullyPaid.Should().BeTrue();
    }

    [Then(@"the payment response should indicate order has remaining balance")]
    public void ThenThePaymentResponseShouldIndicateOrderHasRemainingBalance()
    {
        _paymentResponse.Should().NotBeNull();
        _paymentResponse!.IsFullyPaid.Should().BeFalse();
        _paymentResponse!.RemainingBalance.Should().BeGreaterThan(0);
    }

    [Then(@"screen order (.*) should have status ""(.*)""")]
    public void ThenScreenOrderShouldHaveStatus(int orderId, string expectedStatus)
    {
        var order = _context.ScreenOrders.Include(o => o.OrderStatus).First(o => o.Id == orderId);
        order.OrderStatus.Status.Should().Be(expectedStatus);
    }

    [Then(@"the retrieved order should not be null")]
    public void ThenTheRetrievedOrderShouldNotBeNull()
    {
        _retrievedOrder.Should().NotBeNull();
    }

    [Then(@"the retrieved order should have id (.*)")]
    public void ThenTheRetrievedOrderShouldHaveId(int expectedId)
    {
        _retrievedOrder.Should().NotBeNull();
        _retrievedOrder!.Id.Should().Be(expectedId);
    }

    [Then(@"the update status operation should succeed")]
    public void ThenTheUpdateStatusOperationShouldSucceed()
    {
        _updateStatusResult.Should().BeTrue();
    }

    [Then(@"the update status operation should fail")]
    public void ThenTheUpdateStatusOperationShouldFail()
    {
        _updateStatusResult.Should().BeFalse();
    }

    [Then(@"the update payment operation should succeed")]
    public void ThenTheUpdatePaymentOperationShouldSucceed()
    {
        _updatePaymentResult.Should().BeTrue();
    }

    [Then(@"the update quantity collected operation should succeed")]
    public void ThenTheUpdateQuantityCollectedOperationShouldSucceed()
    {
        _updateQuantityResult.Should().BeTrue();
    }

    [Then(@"screen order (.*) should have quantity collected (.*)")]
    public void ThenScreenOrderShouldHaveQuantityCollected(int orderId, int expectedQuantity)
    {
        var order = _context.ScreenOrders.First(o => o.Id == orderId);
        order.QuantityCollected.Should().Be(expectedQuantity);
    }

    [Then(@"the retrieved orders should contain (.*) items")]
    public void ThenTheRetrievedOrdersShouldContainItems(int expectedCount)
    {
        _retrievedOrders.Should().NotBeNull();
        _retrievedOrders!.Count.Should().Be(expectedCount);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        _caughtException = null;
        _createdOrder = null;
        _retrievedOrder = null;
        _retrievedOrders = null;
        _paymentResponse = null;
    }
}