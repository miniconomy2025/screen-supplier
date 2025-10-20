using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Reqnroll;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Binding]
public class ProductionHistorySteps
{
    private ScreenContext _context;
    private ProductionHistoryService _service;
    private ProductionHistory _result;
    private DateTime _testDate;

    private Mock<IMaterialService> _mockMaterialService;
    private Mock<IProductService> _mockProductService;
    private Mock<IEquipmentService> _mockEquipmentService;
    private readonly Mock<ISimulationTimeService> _mockTimeService;
    private readonly Mock<ISimulationTimeProvider> _mockTimeProvider;
    private Mock<ILogger<ProductionHistoryService>> _mockLogger;

    public ProductionHistorySteps()
    {
        var options = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ScreenContext(options);

        _mockLogger = new Mock<ILogger<ProductionHistoryService>>();
        _mockMaterialService = new Mock<IMaterialService>();
        _mockProductService = new Mock<IProductService>();

        _mockEquipmentService = new Mock<IEquipmentService>();

        _mockEquipmentService.Setup(e => e.GetActiveEquipmentAsync())
            .ReturnsAsync(new List<Equipment>
            {
            new Equipment
            {
                IsAvailable = true,
                IsProducing = true,
                EquipmentParameters = new EquipmentParameters { OutputScreens = 100 }
            }
            });

        _mockTimeService = new Mock<ISimulationTimeService>();
        _mockTimeProvider = new Mock<ISimulationTimeProvider>();

        _service = new ProductionHistoryService(
            _context,
            _mockLogger.Object,
            _mockMaterialService.Object,
            _mockProductService.Object,
            _mockEquipmentService.Object,
            _mockTimeProvider.Object
        );
    }

    [Given(@"a fresh in-memory database")]
    public void GivenAFreshDatabase()
    {
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    [Given(@"a production history record exists for ""(.*)""")]
    public async Task GivenAProductionHistoryRecordExistsFor(string date)
    {
        _testDate = DateTime.Parse(date);
        _context.ProductionHistory.Add(new ProductionHistory
        {
            RecordDate = _testDate,
            ScreensProduced = 50,
            SandStock = 100,
            CopperStock = 200,
            ScreenStock = 300,
            ScreenPrice = 400,
            WorkingEquipment = 5
        });
        await _context.SaveChangesAsync();
    }

    [Given(@"no production history exists for ""(.*)""")]
    public async Task GivenNoProductionHistoryExistsFor(string date)
    {
        _testDate = DateTime.Parse(date);
        var existing = _context.ProductionHistory.Where(p => p.RecordDate == _testDate);
        _context.ProductionHistory.RemoveRange(existing);
        await _context.SaveChangesAsync();
    }

    [Given(@"materials, products, and equipment exist in the system")]
    public void GivenMaterialsProductsAndEquipmentExist()
    {
        _mockMaterialService.Setup(m => m.GetAllMaterialsAsync())
            .ReturnsAsync(new List<Material>
            {
                new Material { Name = "sand", Quantity = 1000 },
                new Material { Name = "copper", Quantity = 500 }
            });

        _mockProductService.Setup(p => p.GetProductsAsync())
            .ReturnsAsync(new List<Product>
            {
                new Product { Id = 1, Quantity = 500, Price = 10 }
            });

        _mockEquipmentService.Setup(e => e.GetActiveEquipmentAsync())
            .ReturnsAsync(new List<Equipment>
            {
                new Equipment
                {
                    IsAvailable = true,
                    IsProducing = true,
                    EquipmentParameters = new EquipmentParameters { OutputScreens = 100 }
                }
            });
    }


}
