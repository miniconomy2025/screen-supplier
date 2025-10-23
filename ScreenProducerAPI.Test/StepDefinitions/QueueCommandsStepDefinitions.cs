using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Command.Queue;
using ScreenProducerAPI.Commands;
using ScreenProducerAPI.Commands.Queue;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
[Scope(Tag = "QueueCommands")]
public sealed class QueueCommandsStepDefinitions
{
    private ScreenContext _context = null!;
    private Mock<IBankService> _mockBankService = null!;
    private Mock<ILogisticsService> _mockLogisticsService = null!;
    private Mock<IEquipmentService> _mockEquipmentService = null!;
    private Mock<IPurchaseOrderService> _mockPurchaseOrderService = null!;
    private Mock<ILogger<ProcessSupplierPaymentCommand>> _mockSupplierPaymentLogger = null!;
    private Mock<ILogger<ProcessLogisticsPaymentCommand>> _mockLogisticsPaymentLogger = null!;
    private Mock<ILogger<ProcessShippingRequestCommand>> _mockShippingRequestLogger = null!;
    private Mock<IOptionsMonitor<CompanyInfoConfig>> _mockCompanyConfig = null!;
    private Mock<IServiceProvider> _mockServiceProvider = null!;
    
    private CommandResult? _commandResult;
    private ICommand<CommandResult>? _currentCommand;
    private string? _capturedErrorMessage;
    private int _capturedPaymentAmount;
    private string? _capturedPaymentDescription;

    [BeforeScenario("QueueCommands")]
    public void SetupMocks()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ScreenContext(options);

        // Seed order statuses
        if (!_context.OrderStatuses.Any())
        {
            _context.OrderStatuses.AddRange(
                new OrderStatus { Id = 1, Status = Status.RequiresPaymentToSupplier },
                new OrderStatus { Id = 2, Status = Status.RequiresDelivery },
                new OrderStatus { Id = 3, Status = Status.RequiresPaymentToLogistics },
                new OrderStatus { Id = 4, Status = Status.WaitingForDelivery },
                new OrderStatus { Id = 5, Status = Status.Delivered },
                new OrderStatus { Id = 6, Status = Status.Abandoned }
            );
            _context.SaveChanges();
        }

        // Setup mocks
        _mockBankService = new Mock<IBankService>();
        _mockLogisticsService = new Mock<ILogisticsService>();
        _mockEquipmentService = new Mock<IEquipmentService>();
        _mockPurchaseOrderService = new Mock<IPurchaseOrderService>();
        _mockSupplierPaymentLogger = new Mock<ILogger<ProcessSupplierPaymentCommand>>();
        _mockLogisticsPaymentLogger = new Mock<ILogger<ProcessLogisticsPaymentCommand>>();
        _mockShippingRequestLogger = new Mock<ILogger<ProcessShippingRequestCommand>>();
        
        _mockCompanyConfig = new Mock<IOptionsMonitor<CompanyInfoConfig>>();
        _mockCompanyConfig.Setup(x => x.CurrentValue).Returns(new CompanyInfoConfig
        {
            CompanyId = "123"
        });

        // Setup service provider
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(IBankService))).Returns(_mockBankService.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogisticsService))).Returns(_mockLogisticsService.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IEquipmentService))).Returns(_mockEquipmentService.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IPurchaseOrderService))).Returns(_mockPurchaseOrderService.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<ProcessSupplierPaymentCommand>))).Returns(_mockSupplierPaymentLogger.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<ProcessLogisticsPaymentCommand>))).Returns(_mockLogisticsPaymentLogger.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<ProcessShippingRequestCommand>))).Returns(_mockShippingRequestLogger.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptionsMonitor<CompanyInfoConfig>))).Returns(_mockCompanyConfig.Object);

        _commandResult = null;
        _capturedErrorMessage = null;
        _capturedPaymentAmount = 0;
        _capturedPaymentDescription = null;
    }

    [Given(@"there is a purchase order with id (.*) in status ""(.*)""")]
    public void GivenThereIsAPurchaseOrderWithIdInStatus(int orderId, string status)
    {
        var orderStatus = _context.OrderStatuses.First(os => os.Status == status);
        var purchaseOrder = new PurchaseOrder
        {
            Id = orderId,
            OrderID = orderId + 1000,
            OrderStatusId = orderStatus.Id,
            OrderStatus = orderStatus,
            Quantity = 100,
            UnitPrice = 50,
            BankAccountNumber = "BANK123",
            Origin = "supplier",
            OrderDate = DateTime.UtcNow
        };
        _context.PurchaseOrders.Add(purchaseOrder);
        _context.SaveChanges();
    }

    [Given(@"the purchase order (.*) has quantity (.*) and unit price (.*)")]
    public void GivenThePurchaseOrderHasQuantityAndUnitPrice(int orderId, int quantity, int unitPrice)
    {
        var purchaseOrder = _context.PurchaseOrders.First(po => po.Id == orderId);
        purchaseOrder.Quantity = quantity;
        purchaseOrder.UnitPrice = unitPrice;
        _context.SaveChanges();
    }

    [Given(@"the purchase order (.*) has shipment id (.*)")]
    public void GivenThePurchaseOrderHasShipmentId(int orderId, int shipmentId)
    {
        var purchaseOrder = _context.PurchaseOrders.First(po => po.Id == orderId);
        purchaseOrder.ShipmentID = shipmentId;
        _context.SaveChanges();
    }

    [Given(@"the purchase order (.*) has shipper bank account ""(.*)""")]
    public void GivenThePurchaseOrderHasShipperBankAccount(int orderId, string bankAccount)
    {
        var purchaseOrder = _context.PurchaseOrders.First(po => po.Id == orderId);
        purchaseOrder.ShipperBankAccout = bankAccount;
        _context.SaveChanges();
    }

    [Given(@"the purchase order (.*) has shipping price (.*)")]
    public void GivenThePurchaseOrderHasShippingPrice(int orderId, int shippingPrice)
    {
        var purchaseOrder = _context.PurchaseOrders.First(po => po.Id == orderId);
        purchaseOrder.OrderShippingPrice = shippingPrice;
        _context.SaveChanges();
    }

    [Given(@"the purchase order (.*) is an equipment order")]
    public void GivenThePurchaseOrderIsAnEquipmentOrder(int orderId)
    {
        var purchaseOrder = _context.PurchaseOrders.First(po => po.Id == orderId);
        purchaseOrder.EquipmentOrder = true;
        _context.SaveChanges();
    }

    [Given(@"the purchase order (.*) is a raw material order with material ""(.*)""")]
    public void GivenThePurchaseOrderIsARawMaterialOrderWithMaterial(int orderId, string materialName)
    {
        var material = _context.Materials.FirstOrDefault(m => m.Name == materialName);
        if (material == null)
        {
            material = new Material
            {
                Name = materialName,
                Quantity = 1000
            };
            _context.Materials.Add(material);
            _context.SaveChanges();
        }

        var purchaseOrder = _context.PurchaseOrders.First(po => po.Id == orderId);
        purchaseOrder.RawMaterialId = material.Id;
        purchaseOrder.RawMaterial = material;
        purchaseOrder.EquipmentOrder = false;
        _context.SaveChanges();
    }

    [Given(@"the bank service will succeed for supplier payment")]
    public void GivenTheBankServiceWillSucceedForSupplierPayment()
    {
        _mockBankService.Setup(x => x.MakePaymentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>()))
            .Callback<string, string, int, string>((_, _, amount, desc) =>
            {
                _capturedPaymentAmount = amount;
                _capturedPaymentDescription = desc;
            })
            .ReturnsAsync(true);

        _mockPurchaseOrderService.Setup(x => x.UpdateStatusAsync(
            It.IsAny<int>(),
            Status.RequiresDelivery))
            .ReturnsAsync(true);
    }

    [Given(@"the bank service will fail for supplier payment")]
    public void GivenTheBankServiceWillFailForSupplierPayment()
    {
        _mockBankService.Setup(x => x.MakePaymentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>()))
            .ReturnsAsync(false);
    }

    [Given(@"the bank service will throw exception for supplier payment")]
    public void GivenTheBankServiceWillThrowExceptionForSupplierPayment()
    {
        _mockBankService.Setup(x => x.MakePaymentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>()))
            .ThrowsAsync(new Exception("Bank service connection error"));
    }

    [Given(@"the bank service will succeed for logistics payment")]
    public void GivenTheBankServiceWillSucceedForLogisticsPayment()
    {
        _mockBankService.Setup(x => x.MakePaymentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>()))
            .Callback<string, string, int, string>((_, _, amount, desc) =>
            {
                _capturedPaymentAmount = amount;
                _capturedPaymentDescription = desc;
            })
            .ReturnsAsync(true);

        _mockPurchaseOrderService.Setup(x => x.UpdateStatusAsync(
            It.IsAny<int>(),
            Status.WaitingForDelivery))
            .ReturnsAsync(true);
    }

    [Given(@"the bank service will fail for logistics payment")]
    public void GivenTheBankServiceWillFailForLogisticsPayment()
    {
        _mockBankService.Setup(x => x.MakePaymentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>()))
            .ReturnsAsync(false);
    }

    [Given(@"the bank service will throw exception for logistics payment")]
    public void GivenTheBankServiceWillThrowExceptionForLogisticsPayment()
    {
        _mockBankService.Setup(x => x.MakePaymentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>()))
            .ThrowsAsync(new Exception("Logistics payment network error"));
    }

    [Given(@"the equipment service has valid parameters")]
    public void GivenTheEquipmentServiceHasValidParameters()
    {
        _mockEquipmentService.Setup(x => x.GetEquipmentParametersAsync())
            .ReturnsAsync(new EquipmentParameters
            {
                EquipmentWeight = 2000,
                OutputScreens = 100,
                InputSandKg = 3,
                InputCopperKg = 2
            });
    }

    [Given(@"the equipment service has no parameters")]
    public void GivenTheEquipmentServiceHasNoParameters()
    {
        _mockEquipmentService.Setup(x => x.GetEquipmentParametersAsync())
            .ReturnsAsync((EquipmentParameters?)null);
    }

    [Given(@"the logistics service will succeed for pickup request")]
    public void GivenTheLogisticsServiceWillSucceedForPickupRequest()
    {
        _mockLogisticsService.Setup(x => x.RequestPickupAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<PickupRequestItem>>()))
            .ReturnsAsync(("12345", "LOGISTICS-BANK-ACC", 500));

        _mockPurchaseOrderService.Setup(x => x.UpdateShipmentIdAsync(
            It.IsAny<int>(),
            It.IsAny<int>()))
            .ReturnsAsync(true);

        _mockPurchaseOrderService.Setup(x => x.UpdateStatusAsync(
            It.IsAny<int>(),
            Status.RequiresPaymentToLogistics))
            .ReturnsAsync(true);

        _mockPurchaseOrderService.Setup(x => x.UpdateOrderShippingDetailsAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<int>()))
            .ReturnsAsync(true);
    }

    [Given(@"the logistics service will throw exception for pickup request")]
    public void GivenTheLogisticsServiceWillThrowExceptionForPickupRequest()
    {
        _mockLogisticsService.Setup(x => x.RequestPickupAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<List<PickupRequestItem>>()))
            .ThrowsAsync(new Exception("Logistics service unavailable"));
    }

    [When(@"I execute ProcessSupplierPaymentCommand for purchase order (.*)")]
    public async Task WhenIExecuteProcessSupplierPaymentCommandForPurchaseOrder(int orderId)
    {
        var purchaseOrder = _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .First(po => po.Id == orderId);

        var command = new ProcessSupplierPaymentCommand(
            purchaseOrder,
            _mockBankService.Object,
            _mockPurchaseOrderService.Object,
            _mockSupplierPaymentLogger.Object);

        try
        {
            _commandResult = await command.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _capturedErrorMessage = ex.Message;
        }
    }

    [When(@"I execute ProcessLogisticsPaymentCommand for purchase order (.*)")]
    public async Task WhenIExecuteProcessLogisticsPaymentCommandForPurchaseOrder(int orderId)
    {
        var purchaseOrder = _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .First(po => po.Id == orderId);

        var command = new ProcessLogisticsPaymentCommand(
            purchaseOrder,
            _mockBankService.Object,
            _mockPurchaseOrderService.Object,
            _mockLogisticsPaymentLogger.Object);

        try
        {
            _commandResult = await command.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _capturedErrorMessage = ex.Message;
        }
    }

    [When(@"I execute ProcessShippingRequestCommand for purchase order (.*)")]
    public async Task WhenIExecuteProcessShippingRequestCommandForPurchaseOrder(int orderId)
    {
        var purchaseOrder = _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .Include(po => po.RawMaterial)
            .First(po => po.Id == orderId);

        var command = new ProcessShippingRequestCommand(
            purchaseOrder,
            _mockLogisticsService.Object,
            _mockPurchaseOrderService.Object,
            _mockEquipmentService.Object,
            _mockCompanyConfig.Object,
            _mockShippingRequestLogger.Object);

        try
        {
            _commandResult = await command.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _capturedErrorMessage = ex.Message;
        }
    }

    [When(@"I execute NoOpCommand for purchase order (.*)")]
    public async Task WhenIExecuteNoOpCommandForPurchaseOrder(int orderId)
    {
        var purchaseOrder = _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .First(po => po.Id == orderId);

        var command = new NoOpCommand(purchaseOrder.OrderStatus.Status);
        _commandResult = await command.ExecuteAsync();
    }

    [When(@"I create a command using the factory for purchase order (.*)")]
    public void WhenICreateACommandUsingTheFactoryForPurchaseOrder(int orderId)
    {
        var purchaseOrder = _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .First(po => po.Id == orderId);

        var factory = new QueueCommandFactory(_mockServiceProvider.Object);
        _currentCommand = factory.CreateCommand(purchaseOrder);
    }

    [Then(@"the command result should be successful")]
    public void ThenTheCommandResultShouldBeSuccessful()
    {
        _commandResult.Should().NotBeNull();
        _commandResult!.Success.Should().BeTrue();
    }

    [Then(@"the command result should be failed")]
    public void ThenTheCommandResultShouldBeFailed()
    {
        _commandResult.Should().NotBeNull();
        _commandResult!.Success.Should().BeFalse();
    }

    [Then(@"the command result should be failed without retry")]
    public void ThenTheCommandResultShouldBeFailedWithoutRetry()
    {
        _commandResult.Should().NotBeNull();
        _commandResult!.Success.Should().BeFalse();
        _commandResult.ShouldRetry.Should().BeFalse();
    }

    [Then(@"the purchase order (.*) status should be ""(.*)""")]
    public void ThenThePurchaseOrderStatusShouldBe(int orderId, string expectedStatus)
    {
        _mockPurchaseOrderService.Verify(
            x => x.UpdateStatusAsync(orderId, expectedStatus),
            Times.Once);
    }

    [Then(@"the purchase order (.*) status should still be ""(.*)""")]
    public void ThenThePurchaseOrderStatusShouldStillBe(int orderId, string expectedStatus)
    {
        _mockPurchaseOrderService.Verify(
            x => x.UpdateStatusAsync(orderId, It.IsAny<string>()),
            Times.Never);
    }

    [Then(@"the error message should contain exception details")]
    public void ThenTheErrorMessageShouldContainExceptionDetails()
    {
        if (_commandResult != null)
        {
            _commandResult.ErrorMessage.Should().NotBeNullOrEmpty();
        }
        else
        {
            _capturedErrorMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Then(@"the error message should contain ""(.*)""")]
    public void ThenTheErrorMessageShouldContain(string expectedMessage)
    {
        if (_commandResult != null)
        {
            _commandResult.ErrorMessage.Should().Contain(expectedMessage);
        }
    }

    [Then(@"the purchase order (.*) should have shipment id")]
    public void ThenThePurchaseOrderShouldHaveShipmentId(int orderId)
    {
        _mockPurchaseOrderService.Verify(
            x => x.UpdateShipmentIdAsync(orderId, It.IsAny<int>()),
            Times.Once);
    }

    [Then(@"the purchase order (.*) should have shipping details")]
    public void ThenThePurchaseOrderShouldHaveShippingDetails(int orderId)
    {
        _mockPurchaseOrderService.Verify(
            x => x.UpdateOrderShippingDetailsAsync(orderId, It.IsAny<string>(), It.IsAny<int>()),
            Times.Once);
    }

    [Then(@"the command should be of type ProcessSupplierPaymentCommand")]
    public void ThenTheCommandShouldBeOfTypeProcessSupplierPaymentCommand()
    {
        _currentCommand.Should().BeOfType<ProcessSupplierPaymentCommand>();
    }

    [Then(@"the command should be of type ProcessShippingRequestCommand")]
    public void ThenTheCommandShouldBeOfTypeProcessShippingRequestCommand()
    {
        _currentCommand.Should().BeOfType<ProcessShippingRequestCommand>();
    }

    [Then(@"the command should be of type ProcessLogisticsPaymentCommand")]
    public void ThenTheCommandShouldBeOfTypeProcessLogisticsPaymentCommand()
    {
        _currentCommand.Should().BeOfType<ProcessLogisticsPaymentCommand>();
    }

    [Then(@"the command should be of type NoOpCommand")]
    public void ThenTheCommandShouldBeOfTypeNoOpCommand()
    {
        _currentCommand.Should().BeOfType<NoOpCommand>();
    }

    [Then(@"the bank service should have been called with amount (.*)")]
    public void ThenTheBankServiceShouldHaveBeenCalledWithAmount(int expectedAmount)
    {
        _capturedPaymentAmount.Should().Be(expectedAmount);
    }

    [Then(@"the bank service should have been called with description ""(.*)""")]
    public void ThenTheBankServiceShouldHaveBeenCalledWithDescription(string expectedDescription)
    {
        _capturedPaymentDescription.Should().Be(expectedDescription);
    }

    [AfterScenario("QueueCommands")]
    public void AfterScenario()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}
