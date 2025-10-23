using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using ScreenProducerAPI.IntegrationTests.Fixtures;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScreenProducerAPI.IntegrationTests.Tests.Services
{
    [TestFixture]
    public class StockStatisticsServiceTests
    {
        private CustomWebApplicationFactory _factory = null!;
        private IServiceScope _scope = null!;
        private StockStatisticsService _service = null!;
        private ScreenContext _context = null!;
        private IEquipmentService _IEquipmentService = null!;
        private IOptionsMonitor<TargetQuantitiesConfig> _targetConfig = null!;
        private IOptionsMonitor<StockManagementOptions> _stockConfig = null!;

        [SetUp]
        public async Task SetUp()
        {
            _factory = new CustomWebApplicationFactory();
            _scope = _factory.Services.CreateScope();

            _context = _scope.ServiceProvider.GetRequiredService<ScreenContext>();
            _IEquipmentService = _scope.ServiceProvider.GetRequiredService<IEquipmentService>();
            _targetConfig = _scope.ServiceProvider.GetRequiredService<IOptionsMonitor<TargetQuantitiesConfig>>();
            _stockConfig = _scope.ServiceProvider.GetRequiredService<IOptionsMonitor<StockManagementOptions>>();

            _service = new StockStatisticsService(_context, _targetConfig, _stockConfig, _IEquipmentService);

            _context.Equipment.RemoveRange(_context.Equipment);
            _context.EquipmentParameters.RemoveRange(_context.EquipmentParameters);
            await _context.SaveChangesAsync();
        }

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _factory?.Dispose();
        }

        [Test]
        public async Task GetMaterialStatisticsAsync_WhenNoEquipment_ReturnsZeroConsumption()
        {
            var stats = await _service.GetMaterialStatisticsAsync();

            stats.Should().NotBeNull();
            stats.Sand.DailyConsumption.Should().Be(0);
            stats.Copper.DailyConsumption.Should().Be(0);
            stats.Sand.ReorderPoint.Should().Be(_targetConfig.CurrentValue.Sand.Target);
        }

        [Test]
        public async Task GetMaterialStatisticsAsync_Computes_ReorderPoints_Correctly()
        {
            var param1 = new EquipmentParameters { Id = 1, InputSandKg = 10, InputCopperKg = 5 };
            var param2 = new EquipmentParameters { Id = 2, InputSandKg = 12, InputCopperKg = 6 };
            _context.EquipmentParameters.AddRange(param1, param2);
            await _context.SaveChangesAsync();

            _context.Equipment.AddRange(new List<Equipment>
            {
                new Equipment { ParametersID = 1, IsProducing = true, IsAvailable = true },
                new Equipment { ParametersID = 2, IsProducing = true, IsAvailable = true }
            });
            await _context.SaveChangesAsync();

            var stats = await _service.GetMaterialStatisticsAsync();

            stats.Should().NotBeNull();

            var machineCount = 2;
            var leadTime = _stockConfig.CurrentValue.LogisticsLeadTimeDays;
            var target = _targetConfig.CurrentValue.Sand.Target;

            var expectedSandDaily = param1.InputSandKg * machineCount;   // match service logic
            var expectedCopperDaily = param1.InputCopperKg * machineCount;

            stats.Sand.DailyConsumption.Should().Be(expectedSandDaily);
            stats.Copper.DailyConsumption.Should().Be(expectedCopperDaily);
            stats.Sand.ReorderPoint.Should().Be((expectedSandDaily * leadTime) + target);
            stats.Copper.ReorderPoint.Should().Be((expectedCopperDaily * leadTime) + target);

        }

        [Test]
        public async Task GetMaterialStatisticsAsync_Returns_Consistent_Results()
        {
            var param = new EquipmentParameters { Id = 1, InputSandKg = 10, InputCopperKg = 5 };
            _context.EquipmentParameters.Add(param);
            _context.Equipment.Add(new Equipment { ParametersID = 1, IsProducing = true, IsAvailable = true });
            await _context.SaveChangesAsync();

            var result1 = await _service.GetMaterialStatisticsAsync();
            var result2 = await _service.GetMaterialStatisticsAsync();

            result1.Should().BeEquivalentTo(result2);
        }
    }
}
