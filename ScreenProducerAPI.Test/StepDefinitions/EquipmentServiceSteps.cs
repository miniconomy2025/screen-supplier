using Microsoft.EntityFrameworkCore;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Models.Responses;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System;

[Binding]
public class EquipmentServiceSteps
{
    private ScreenContext _context;
    private Mock<IMaterialService> _mockMaterialService;
    private Mock<IProductService> _mockProductService;
    private EquipmentService _equipmentService;
    private bool _boolResult;
    private int _intResult;
    private MachineFailureResponse _failureResponse;

    public EquipmentServiceSteps()
    {
        var options = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ScreenContext(options);

        _mockMaterialService = new Mock<IMaterialService>();
        _mockProductService = new Mock<IProductService>();

        _equipmentService = new EquipmentService(_context, _mockMaterialService.Object, _mockProductService.Object);
    }

    [Given(@"there are no existing equipment parameters")]
    public async Task GivenThereAreNoExistingEquipmentParameters()
    {
        _context.EquipmentParameters.RemoveRange(_context.EquipmentParameters);
        await _context.SaveChangesAsync();
    }

    [When(@"I initialize the equipment parameters with input sand (.*), copper (.*), output screens (.*), and weight (.*)")]
    public async Task WhenIInitializeTheEquipmentParameters(int sand, int copper, int screens, int weight)
    {
        _boolResult = await _equipmentService.InitializeEquipmentParametersAsync(sand, copper, screens, weight);
    }

    [Then(@"the initialization should be successful")]
    public void ThenTheInitializationShouldBeSuccessful()
    {
        Assert.That(_boolResult, Is.True);
        Assert.That(_context.EquipmentParameters.Any(), Is.True);
    }

    [Given(@"valid equipment parameters exist")]
    public async Task GivenValidEquipmentParametersExist()
    {
        await _equipmentService.InitializeEquipmentParametersAsync(5, 10, 20, 100);
    }

    [When(@"I add equipment for purchase order (.*)")]
    public async Task WhenIAddEquipmentForPurchaseOrder(int purchaseOrderId)
    {
        _boolResult = await _equipmentService.AddEquipmentAsync(purchaseOrderId);
    }

    [Then(@"the equipment should be added successfully")]
    public void ThenTheEquipmentShouldBeAddedSuccessfully()
    {
        Assert.That(_boolResult, Is.True);
        Assert.That(_context.Equipment.Count(), Is.GreaterThan(0));
    }

    [Given(@"there are available machines")]
    public async Task GivenThereAreAvailableMachines()
    {
        if (!await _context.EquipmentParameters.AnyAsync())
        {
            await _equipmentService.InitializeEquipmentParametersAsync(5, 10, 20, 100);
        }

        var param = await _context.EquipmentParameters.FirstAsync();
        for (int i = 0; i < 3; i++)
        {
            _context.Equipment.Add(new Equipment
            {
                ParametersID = param.Id,
                IsAvailable = true,
                IsProducing = false
            });
        }
        await _context.SaveChangesAsync();
    }

    [Given(@"sufficient materials are available")]
    public void GivenSufficientMaterialsAreAvailable()
    {
        _mockMaterialService
            .Setup(m => m.HasSufficientMaterialsAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);
        _mockMaterialService
            .Setup(m => m.ConsumeMaterialAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);
    }

    [When(@"I start production")]
    public async Task WhenIStartProduction()
    {
        _intResult = await _equipmentService.StartProductionAsync();
    }

    [Then(@"some machines should start producing")]
    public void ThenSomeMachinesShouldStartProducing()
    {
        Assert.That(_intResult, Is.GreaterThan(0));
        Assert.That(_context.Equipment.Any(e => e.IsProducing), Is.True);
    }

    
}
