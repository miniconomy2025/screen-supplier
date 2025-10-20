using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;
using ScreenProducerAPI.Services.SupplierService.Recycler.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
public sealed class ReorderServiceStepDefinitions
{
    private Mock<ITargetQuantityService> _mockTargetQuantityService;
    private Mock<IPurchaseOrderService> _mockPurchaseOrderService;
    private Mock<IPurchaseOrderQueueService> _mockQueueService;
    private Mock<IProductService> _mockProductService;
    private Mock<IMaterialService> _mockMaterialService;
    private Mock<IEquipmentService> _mockEquipmentService;
    private Mock<IBankService> _mockBankService;
    private Mock<IHandService> _mockHandService;
    private Mock<IRecyclerService> _mockRecyclerService;
    private Mock<IOptionsMonitor<TargetQuantitiesConfig>> _mockTargetConfig;
    private Mock<IOptionsMonitor<ReorderSettingsConfig>> _mockReorderConfig;
    private Mock<ILogger<ReorderService>> _mockLogger;

    private ReorderService _reorderService;
    private ReorderService.ReorderResult _result;

    private ReorderSettingsConfig _reorderSettings;
    private TargetQuantitiesConfig _targetQuantities;
    private InventoryStatus _inventoryStatus;

    public ReorderServiceStepDefinitions()
    {
        SetupMocks();
        _reorderService = new ReorderService(
            _mockTargetQuantityService.Object,
            _mockPurchaseOrderService.Object,
            _mockQueueService.Object,
            _mockProductService.Object,
            _mockMaterialService.Object,
            _mockEquipmentService.Object,
            _mockBankService.Object,
            _mockHandService.Object,
            _mockRecyclerService.Object,
            _mockLogger.Object,
            _mockTargetConfig.Object,
            _mockReorderConfig.Object);
    }

    private void SetupMocks()
    {
        _mockTargetQuantityService = new Mock<ITargetQuantityService>();
        _mockPurchaseOrderService = new Mock<IPurchaseOrderService>();
        _mockQueueService = new Mock<IPurchaseOrderQueueService>();
        _mockProductService = new Mock<IProductService>();
        _mockMaterialService = new Mock<IMaterialService>();
        _mockEquipmentService = new Mock<IEquipmentService>();
        _mockBankService = new Mock<IBankService>();
        _mockHandService = new Mock<IHandService>();
        _mockRecyclerService = new Mock<IRecyclerService>();
        _mockTargetConfig = new Mock<IOptionsMonitor<TargetQuantitiesConfig>>();
        _mockReorderConfig = new Mock<IOptionsMonitor<ReorderSettingsConfig>>();
        _mockLogger = new Mock<ILogger<ReorderService>>();

        // Default configurations
        _reorderSettings = new ReorderSettingsConfig
        {
            EnableAutoReorder = true,
            EnableScreenStockCheck = false,
            MaxScreensBeforeStopOrdering = 1000
        };

        _targetQuantities = new TargetQuantitiesConfig
        {
            Sand = new TargetQuantityConfig { Target = 1000, OrderQuantity = 500, ReorderPoint = 200 },
            Copper = new TargetQuantityConfig { Target = 1000, OrderQuantity = 500, ReorderPoint = 200 },
            Equipment = new TargetQuantityConfig { Target = 5, OrderQuantity = 1, ReorderPoint = 2 }
        };

        _inventoryStatus = new InventoryStatus
        {
            Sand = new MaterialStatus { NeedsReorder = false },
            Copper = new MaterialStatus { NeedsReorder = false },
            Equipment = new EquipmentStatus { NeedsReorder = false, Incoming = 0 }
        };

        _mockReorderConfig.Setup(x => x.CurrentValue).Returns(_reorderSettings);
        _mockTargetConfig.Setup(x => x.CurrentValue).Returns(_targetQuantities);
        _mockTargetQuantityService.Setup(x => x.GetInventoryStatusAsync()).ReturnsAsync(_inventoryStatus);
    }

    [Given(@"auto reorder is (disabled|enabled) in configuration", ExpressionType = ExpressionType.RegularExpression)]
    public void GivenAutoReorderIsDisabledInConfiguration(string enabled)
    {
        _reorderSettings.EnableAutoReorder = enabled == "enabled";
    }

    [Given(@"screen stock check is (disabled|enabled)", ExpressionType = ExpressionType.RegularExpression)]
    public void GivenScreenStockCheckIsEnabled(string enabled)
    {
        _reorderSettings.EnableScreenStockCheck = enabled == "enabled";
    }

    [Given(@"available screens in stock is (.*)")]
    public void GivenAvailableScreensInStockIs(int screenCount)
    {
        _mockProductService.Setup(x => x.GetAvailableStockAsync()).ReturnsAsync(screenCount);
    }

    [Given(@"max screens before stop ordering is (.*)")]
    public void GivenMaxScreensBeforeStopOrderingIs(int maxScreens)
    {
        _reorderSettings.MaxScreensBeforeStopOrdering = maxScreens;
    }

    [Given(@"there are no working machines available")]
    public void GivenThereAreNoWorkingMachinesAvailable()
    {
        _mockEquipmentService.Setup(x => x.GetAllEquipmentAsync())
            .ReturnsAsync(new List<Equipment>());
    }

    [Given(@"there are (.*) working machines available")]
    public void GivenThereAreWorkingMachinesAvailable(int machineCount)
    {
        var equipment = new List<Equipment>();
        for (int i = 0; i < machineCount; i++)
        {
            equipment.Add(new Equipment { IsAvailable = true });
        }
        _mockEquipmentService.Setup(x => x.GetAllEquipmentAsync()).ReturnsAsync(equipment);
    }

    [Given(@"equipment (needs|does not need) reorder", ExpressionType = ExpressionType.RegularExpression)]
    public void GivenEquipmentReorderStatus(string requirement)
    {
        _inventoryStatus.Equipment.NeedsReorder = requirement == "needs";
    }

    [Given(@"(sand|copper) (needs|does not need) reorder", ExpressionType = ExpressionType.RegularExpression)]
    public void MaterialReorderRequirement(string material, string requirement)
    {
        var needsReorder = requirement == "needs";
        switch (material.ToLower())
        {
            case "sand":
                _inventoryStatus.Sand.NeedsReorder = needsReorder;
                break;
            case "copper":
                _inventoryStatus.Copper.NeedsReorder = needsReorder;
                break;
            default:
                throw new ArgumentException($"Unknown material: {material}");
        }
    }

    [Given(@"emergency machine can be afforded")]
    public void GivenEmergencyMachineCanBeAfforded()
    {
        var machine = new MachineForSale { MachineName = "screen_machine", Price = 1000, Quantity = 5 };
        var machinesResponse = new MachinesForSaleResponse { Machines = new List<MachineForSale> { machine } };

        _mockHandService.Setup(x => x.GetMachinesForSaleAsync()).ReturnsAsync(machinesResponse);
        _mockBankService.Setup(x => x.HasSufficientBalanceAsync(1000)).ReturnsAsync(true);

        // Setup equipment purchase
        var purchaseResponse = new PurchaseMachineResponse
        {
            TotalPrice = 1000,
            BankAccount = "emergency-account",
            OrderId = 99
        };
        _mockHandService.Setup(x => x.PurchaseMachineAsync(It.IsAny<PurchaseMachineRequest>()))
            .ReturnsAsync(purchaseResponse);

        var equipmentOrder = new PurchaseOrder { Id = 99 };
        _mockPurchaseOrderService.Setup(x => x.CreatePurchaseOrderAsync(99, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null, true))
            .ReturnsAsync(equipmentOrder);
    }

    [Given(@"materials can be afforded")]
    public void GivenMaterialsCanBeAfforded()
    {
        // Setup successful material purchases
        var sandMaterial = new RawMaterialForSale { RawMaterialName = "sand", PricePerKg = 10 };
        var copperMaterial = new RawMaterialForSale { RawMaterialName = "copper", PricePerKg = 20 };
        var materials = new List<RawMaterialForSale> { sandMaterial, copperMaterial };

        _mockHandService.Setup(x => x.GetRawMaterialsForSaleAsync()).ReturnsAsync(materials);
        _mockBankService.Setup(x => x.HasSufficientBalanceAsync(It.IsAny<int>())).ReturnsAsync(true);

        // Setup successful purchase responses
        var sandPurchaseResponse = new PurchaseRawMaterialResponse
        {
            Price = 5000,
            BankAccount = "test-account",
            OrderId = 1
        };
        var copperPurchaseResponse = new PurchaseRawMaterialResponse
        {
            Price = 10000,
            BankAccount = "test-account",
            OrderId = 2
        };

        _mockHandService.Setup(x => x.PurchaseRawMaterialAsync(It.Is<PurchaseRawMaterialRequest>(r => r.MaterialName == "sand")))
            .ReturnsAsync(sandPurchaseResponse);
        _mockHandService.Setup(x => x.PurchaseRawMaterialAsync(It.Is<PurchaseRawMaterialRequest>(r => r.MaterialName == "copper")))
            .ReturnsAsync(copperPurchaseResponse);

        // Setup material service responses
        var sandMaterialEntity = new Material { Id = 1, Name = "sand" };
        var copperMaterialEntity = new Material { Id = 2, Name = "copper" };

        _mockMaterialService.Setup(x => x.GetMaterialAsync("sand")).ReturnsAsync(sandMaterialEntity);
        _mockMaterialService.Setup(x => x.GetMaterialAsync("copper")).ReturnsAsync(copperMaterialEntity);

        // Setup purchase order creation
        var sandOrder = new PurchaseOrder { Id = 1 };
        var copperOrder = new PurchaseOrder { Id = 2 };

        _mockPurchaseOrderService.Setup(x => x.CreatePurchaseOrderAsync(1, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), 1, false))
            .ReturnsAsync(sandOrder);
        _mockPurchaseOrderService.Setup(x => x.CreatePurchaseOrderAsync(2, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), 2, false))
            .ReturnsAsync(copperOrder);
    }

    [Given(@"new machine should be ordered based on materials and finances")]
    public void GivenNewMachineShouldBeOrderedBasedOnMaterialsAndFinances()
    {
        // Setup sufficient materials and finances for new machine
        var sandMaterial = new Material { Quantity = 10000 };
        var copperMaterial = new Material { Quantity = 10000 };
        var equipmentParams = new EquipmentParameters { InputSandKg = 10, InputCopperKg = 5 };

        _mockMaterialService.Setup(x => x.GetMaterialAsync("sand")).ReturnsAsync(sandMaterial);
        _mockMaterialService.Setup(x => x.GetMaterialAsync("copper")).ReturnsAsync(copperMaterial);
        _mockEquipmentService.Setup(x => x.GetEquipmentParametersAsync()).ReturnsAsync(equipmentParams);
        _mockMaterialService.Setup(x => x.GetAverageCostPerKgAsync(It.IsAny<string>())).ReturnsAsync(10);

        var machine = new MachineForSale { MachineName = "screen_machine", Price = 1000, Quantity = 5 };
        var machinesResponse = new MachinesForSaleResponse { Machines = new List<MachineForSale> { machine } };

        _mockHandService.Setup(x => x.GetMachinesForSaleAsync()).ReturnsAsync(machinesResponse);
        _mockBankService.Setup(x => x.HasSufficientBalanceAsync(It.IsAny<int>())).ReturnsAsync(true);

        // Setup equipment purchase
        var purchaseResponse = new PurchaseMachineResponse
        {
            TotalPrice = 1000,
            BankAccount = "test-account",
            OrderId = 3
        };
        _mockHandService.Setup(x => x.PurchaseMachineAsync(It.IsAny<PurchaseMachineRequest>()))
            .ReturnsAsync(purchaseResponse);

        var equipmentOrder = new PurchaseOrder { Id = 3 };
        _mockPurchaseOrderService.Setup(x => x.CreatePurchaseOrderAsync(3, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null, true))
            .ReturnsAsync(equipmentOrder);
    }

    [Given(@"new machine should not be ordered due to insufficient materials")]
    public void GivenNewMachineShouldNotBeOrderedDueToInsufficientMaterials()
    {
        // Setup insufficient materials
        var sandMaterial = new Material { Quantity = 100 };
        var copperMaterial = new Material { Quantity = 50 };
        var equipmentParams = new EquipmentParameters { InputSandKg = 100, InputCopperKg = 50 };

        _mockMaterialService.Setup(x => x.GetMaterialAsync("sand")).ReturnsAsync(sandMaterial);
        _mockMaterialService.Setup(x => x.GetMaterialAsync("copper")).ReturnsAsync(copperMaterial);
        _mockEquipmentService.Setup(x => x.GetEquipmentParametersAsync()).ReturnsAsync(equipmentParams);

        // Setup machine availability but insufficient funds after calculation
        var machine = new MachineForSale { MachineName = "screen_machine", Price = 1000, Quantity = 5 };
        var machinesResponse = new MachinesForSaleResponse { Machines = new List<MachineForSale> { machine } };

        _mockHandService.Setup(x => x.GetMachinesForSaleAsync()).ReturnsAsync(machinesResponse);
        _mockMaterialService.Setup(x => x.GetAverageCostPerKgAsync(It.IsAny<string>())).ReturnsAsync(10);
    }

    [Given(@"material supplier is not available for sand")]
    public void GivenMaterialSupplierIsNotAvailableForSand()
    {
        _mockHandService.Setup(x => x.GetRawMaterialsForSaleAsync()).ReturnsAsync(new List<RawMaterialForSale>());
        _mockRecyclerService.Setup(x => x.GetMaterialsAsync()).ReturnsAsync(new List<RecyclerMaterial>());
    }

    [When(@"the reorder service checks and processes reorders")]
    public async Task WhenTheReorderServiceChecksAndProcessesReorders()
    {
        _result = await _reorderService.CheckAndProcessReordersAsync();
    }

    [Then(@"auto reorder should be disabled in the result")]
    public void ThenAutoReorderShouldBeDisabledInTheResult()
    {
        _result.AutoReorderEnabled.Should().BeFalse();
    }

    [Then(@"no orders should be created")]
    public void ThenNoOrdersShouldBeCreated()
    {
        _result.SandOrderCreated.Should().BeFalse();
        _result.CopperOrderCreated.Should().BeFalse();
        _result.EquipmentOrderCreated.Should().BeFalse();
    }

    [Then(@"an equipment order should be created")]
    public void ThenAnEquipmentOrderShouldBeCreated()
    {
        _result.EquipmentOrderCreated.Should().BeTrue();
        _result.EquipmentOrderId.Should().NotBeNull();
    }

    [Then(@"it should be logged as emergency equipment order")]
    public void ThenItShouldBeLoggedAsEmergencyEquipmentOrder()
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("EMERGENCY equipment order created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Then(@"a sand order should be created")]
    public void ThenASandOrderShouldBeCreated()
    {
        _result.SandOrderCreated.Should().BeTrue();
        _result.SandOrderId.Should().NotBeNull();
    }

    [Then(@"a copper order should be created")]
    public void ThenACopperOrderShouldBeCreated()
    {
        _result.CopperOrderCreated.Should().BeTrue();
        _result.CopperOrderId.Should().NotBeNull();
    }

    [Then(@"no equipment order should be created")]
    public void ThenNoEquipmentOrderShouldBeCreated()
    {
        _result.EquipmentOrderCreated.Should().BeFalse();
    }

    [Then(@"no sand order should be created")]
    public void ThenNoSandOrderShouldBeCreated()
    {
        _result.SandOrderCreated.Should().BeFalse();
    }

    [Then(@"the service should log screen stock limit reached")]
    public void ThenTheServiceShouldLogScreenStockLimitReached()
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Screen stock limit reached")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Then(@"the service should log skipping equipment order")]
    public void ThenTheServiceShouldLogSkippingEquipmentOrder()
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Skipping equipment order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Then(@"the service should log no suitable supplier found")]
    public void ThenTheServiceShouldLogNoSuitableSupplierFound()
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No suitable sand supplier found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        // Reset for next test
        SetupMocks();
        _reorderService = new ReorderService(
            _mockTargetQuantityService.Object,
            _mockPurchaseOrderService.Object,
            _mockQueueService.Object,
            _mockProductService.Object,
            _mockMaterialService.Object,
            _mockEquipmentService.Object,
            _mockBankService.Object,
            _mockHandService.Object,
            _mockRecyclerService.Object,
            _mockLogger.Object,
            _mockTargetConfig.Object,
            _mockReorderConfig.Object);
    }
}