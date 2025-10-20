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
