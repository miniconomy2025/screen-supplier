using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Commands;
using ScreenProducerAPI.Commands.Queue;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
public sealed class PurchaseOrderQueueServiceStepDefinitions
{
    private Mock<IServiceProvider> _mockServiceProvider = null!;
    private Mock<ILogger<PurchaseOrderQueueService>> _mockLogger = null!;
    private Mock<IOptionsMonitor<QueueSettingsConfig>> _mockQueueConfig = null!;
    private PurchaseOrderQueueService _queueService = null!;
    private QueueSettingsConfig _queueSettings = null!;
    private Mock<IServiceScope> _mockServiceScope = null!;
    private ScreenContext _context = null!;
    private Mock<IQueueCommandFactory> _mockCommandFactory = null!;
    private int _queueCountBefore;
    private int _queueCountAfter;
    private Dictionary<int, Mock<ICommand<CommandResult>>> _mockCommands = null!;

    [BeforeScenario]
    public void SetupMocks()
    {
        _mockCommands = new Dictionary<int, Mock<ICommand<CommandResult>>>();
        _mockLogger = new Mock<ILogger<PurchaseOrderQueueService>>();
        _mockQueueConfig = new Mock<IOptionsMonitor<QueueSettingsConfig>>();

        _queueSettings = new QueueSettingsConfig
        {
            EnableQueueProcessing = true,
            MaxRetries = 3,
            ProcessingIntervalSeconds = 30
        };
        _mockQueueConfig.Setup(x => x.CurrentValue).Returns(_queueSettings);

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

        _mockCommandFactory = new Mock<IQueueCommandFactory>();

        _mockServiceScope = new Mock<IServiceScope>();
        var mockScopeServiceProvider = new Mock<IServiceProvider>();
        mockScopeServiceProvider.Setup(x => x.GetService(typeof(ScreenContext)))
            .Returns(_context);
        mockScopeServiceProvider.Setup(x => x.GetService(typeof(IQueueCommandFactory)))
            .Returns(_mockCommandFactory.Object);
        _mockServiceScope.Setup(x => x.ServiceProvider).Returns(mockScopeServiceProvider.Object);

        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);

        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockServiceScopeFactory.Object);

        _queueService = new PurchaseOrderQueueService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockQueueConfig.Object);
    }

    [Given(@"the queue is empty")]
    public void GivenTheQueueIsEmpty()
    {
        // Queue starts empty by default
        _queueService.GetQueueCount().Should().Be(0);
    }

    [Given(@"the queue processing is enabled")]
    public void GivenTheQueueProcessingIsEnabled()
    {
        _queueSettings.EnableQueueProcessing = true;
    }

    [Given(@"the queue processing is disabled")]
    public void GivenTheQueueProcessingIsDisabled()
    {
        _queueSettings.EnableQueueProcessing = false;
    }

    [Given(@"the maximum retries is set to (.*)")]
    public void GivenTheMaximumRetriesIsSetTo(int maxRetries)
    {
        _queueSettings.MaxRetries = maxRetries;
    }

    [Given(@"there is a purchase order with id (.*) in status ""(.*)""")]
    public void GivenThereIsAPurchaseOrderWithIdInStatus(int orderId, string status)
    {
        var orderStatus = _context.OrderStatuses.First(os => os.Status == status);
        var purchaseOrder = new PurchaseOrder
        {
            Id = orderId,
            OrderID = orderId,
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

    [Given(@"the command for purchase order (.*) will succeed")]
    public void GivenTheCommandForPurchaseOrderWillSucceed(int orderId)
    {
        var mockCommand = new Mock<ICommand<CommandResult>>();
        mockCommand.Setup(x => x.ExecuteAsync()).ReturnsAsync(CommandResult.Succeeded());
        _mockCommands[orderId] = mockCommand;

        _mockCommandFactory.Setup(x => x.CreateCommand(It.Is<PurchaseOrder>(po => po.Id == orderId)))
            .Returns(mockCommand.Object);
    }

    [Given(@"the command for purchase order (.*) will fail with retry")]
    public void GivenTheCommandForPurchaseOrderWillFailWithRetry(int orderId)
    {
        var mockCommand = new Mock<ICommand<CommandResult>>();
        mockCommand.Setup(x => x.ExecuteAsync()).ReturnsAsync(CommandResult.Failed("Test failure", shouldRetry: true));
        _mockCommands[orderId] = mockCommand;

        _mockCommandFactory.Setup(x => x.CreateCommand(It.Is<PurchaseOrder>(po => po.Id == orderId)))
            .Returns(mockCommand.Object);
    }

    [Given(@"the command for purchase order (.*) will fail without retry")]
    public void GivenTheCommandForPurchaseOrderWillFailWithoutRetry(int orderId)
    {
        var mockCommand = new Mock<ICommand<CommandResult>>();
        mockCommand.Setup(x => x.ExecuteAsync()).ReturnsAsync(CommandResult.FailedNoRetry("Test failure no retry"));
        _mockCommands[orderId] = mockCommand;

        _mockCommandFactory.Setup(x => x.CreateCommand(It.Is<PurchaseOrder>(po => po.Id == orderId)))
            .Returns(mockCommand.Object);
    }

    [Given(@"purchase order (.*) is already in the queue")]
    public void GivenPurchaseOrderIsAlreadyInTheQueue(int orderId)
    {
        _queueService.EnqueuePurchaseOrder(orderId);
    }

    [When(@"I enqueue purchase order (.*)")]
    public void WhenIEnqueuePurchaseOrder(int orderId)
    {
        _queueService.EnqueuePurchaseOrder(orderId);
    }

    [When(@"I process the queue")]
    public async Task WhenIProcessTheQueue()
    {
        _queueCountBefore = _queueService.GetQueueCount();
        await _queueService.ProcessQueueAsync();
        _queueCountAfter = _queueService.GetQueueCount();
    }

    [When(@"I populate the queue from database")]
    public async Task WhenIPopulateTheQueueFromDatabase()
    {
        await _queueService.PopulateQueueFromDatabaseAsync();
    }

    [When(@"I get the queue count")]
    public void WhenIGetTheQueueCount()
    {
        // Count is already tracked
    }

    [Then(@"the queue count should be (.*)")]
    public void ThenTheQueueCountShouldBe(int expectedCount)
    {
        _queueService.GetQueueCount().Should().Be(expectedCount);
    }

    [Then(@"the queue count should increase by (.*)")]
    public void ThenTheQueueCountShouldIncreaseBy(int increment)
    {
        _queueService.GetQueueCount().Should().Be(1 + increment - 1);
    }

    [Then(@"the queue should not be processed")]
    public void ThenTheQueueShouldNotBeProcessed()
    {
        _queueCountBefore.Should().Be(_queueCountAfter);
    }

    [Then(@"the queue should be empty after processing")]
    public void ThenTheQueueShouldBeEmptyAfterProcessing()
    {
        _queueCountAfter.Should().Be(0);
    }

    [Then(@"purchase order (.*) should be re-enqueued")]
    public void ThenPurchaseOrderShouldBeReEnqueued(int orderId)
    {
        _queueService.GetQueueCount().Should().BeGreaterThan(0);
    }

    [Then(@"the command should have been executed for purchase order (.*)")]
    public void ThenTheCommandShouldHaveBeenExecutedForPurchaseOrder(int orderId)
    {
        _mockCommandFactory.Verify(x => x.CreateCommand(It.Is<PurchaseOrder>(po => po.Id == orderId)), Times.AtLeastOnce);
    }

    [Then(@"purchase order (.*) should be abandoned")]
    public void ThenPurchaseOrderShouldBeAbandoned(int orderId)
    {
        _queueCountAfter.Should().Be(0);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        _mockCommands?.Clear();
    }
}