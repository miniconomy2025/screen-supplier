using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Reqnroll;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Tests.StepDefinitions;

[Binding]
public class TargetQuantitySteps
{
    private ScreenContext _context;
    private TargetQuantityService _service;
    private InventoryStatus _result;

    private Mock<IOptionsMonitor<TargetQuantitiesConfig>> _mockTargetConfig;
    private Mock<IStockStatisticsService> _mockStockStatistics;

    [Given(@"the current quantities of sand, copper, and equipment are above their reorder points")]
    public async Task GivenCurrentQuantitiesAreAboveReorderPoints()
    {
        var options = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ScreenContext(options);

        // Seed Materials
        _context.Materials.AddRange(
            new Material { Name = "sand", Quantity = 150 },
            new Material { Name = "copper", Quantity = 200 }
        );

        // Seed Equipment
        for(int i = 0; i < 101; i++)
{
            _context.Equipment.Add(new Equipment { IsAvailable = true });
        }

        await _context.SaveChangesAsync();

        SetupMocks(reorderPoint: 100, target: 200);
        _service = new TargetQuantityService(_context, Mock.Of<ILogger<TargetQuantityService>>(), _mockTargetConfig.Object, _mockStockStatistics.Object);
    }

    [Given(@"the current quantity of sand is below its reorder point")]
    public async Task GivenCurrentQuantityOfSandIsBelowReorderPoint()
    {
        var options = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ScreenContext(options);

        _context.Materials.AddRange(
            new Material { Name = "sand", Quantity = 20 },
            new Material { Name = "copper", Quantity = 200 }
        );

        _context.Equipment.Add(new Equipment { IsAvailable = true });

        await _context.SaveChangesAsync();

        SetupMocks(reorderPoint: 1, target: 200);
        _service = new TargetQuantityService(_context, Mock.Of<ILogger<TargetQuantityService>>(), _mockTargetConfig.Object, _mockStockStatistics.Object);
    }

    [Given(@"some materials are below their reorder points")]
    public async Task GivenSomeMaterialsAreBelowTheirReorderPoints()
    {
        var options = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ScreenContext(options);

        // Sand below reorder point
        _context.Materials.Add(new Material { Name = "sand", Quantity = 50 });   // below reorder (100)
                                                                                 // Copper above reorder point
        _context.Materials.Add(new Material { Name = "copper", Quantity = 200 }); // above reorder

        // Equipment above reorder point
        for (int i = 0; i < 120; i++)
        {
            _context.Equipment.Add(new Equipment { IsAvailable = true });
        }

        await _context.SaveChangesAsync();

        SetupMocks(reorderPoint: 100, target: 200);

        _service = new TargetQuantityService(
            _context,
            Mock.Of<ILogger<TargetQuantityService>>(),
            _mockTargetConfig.Object,
            _mockStockStatistics.Object);
    }


    

    private void SetupMocks(int reorderPoint, int target)
    {
        _mockTargetConfig = new Mock<IOptionsMonitor<TargetQuantitiesConfig>>();
        _mockTargetConfig.Setup(x => x.CurrentValue).Returns(new TargetQuantitiesConfig
        {
            Sand = new TargetQuantityConfig { Target = target },
            Copper = new TargetQuantityConfig { Target = target },
            Equipment = new TargetQuantityConfig { Target = target, ReorderPoint = reorderPoint }
        });

        var mockMaterialStats = new AllMaterialStatistics
        {
            Sand = new MaterialStatistics { ReorderPoint = reorderPoint },
            Copper = new MaterialStatistics { ReorderPoint = reorderPoint }
        };

        _mockStockStatistics = new Mock<IStockStatisticsService>();
        _mockStockStatistics
            .Setup(s => s.GetMaterialStatisticsAsync())
            .ReturnsAsync(mockMaterialStats);
    }
}
