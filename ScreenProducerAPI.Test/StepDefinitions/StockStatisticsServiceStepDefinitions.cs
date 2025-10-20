using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Test.StepDefinitions
{
    [Binding]
    public sealed class StockStatisticsServiceStepDefinitions
    {
        private Mock<IEquipmentService> _mockEquipmentService = null!;
        private Mock<IOptionsMonitor<TargetQuantitiesConfig>> _mockTargetConfig = null!;
        private Mock<IOptionsMonitor<StockManagementOptions>> _mockStockConfig = null!;
        private StockStatisticsService _service = null!;

        private List<Equipment> _equipmentList = null!;
        private EquipmentParameters _equipmentParameters = null!;
        private AllMaterialStatistics _statistics = null!;

        [BeforeScenario]
        public void Setup()
        {
            // Initialize mocks
            _mockEquipmentService = new Mock<IEquipmentService>();
            _mockTargetConfig = new Mock<IOptionsMonitor<TargetQuantitiesConfig>>();
            _mockStockConfig = new Mock<IOptionsMonitor<StockManagementOptions>>();

            // Initialize service with mocks
            _service = new StockStatisticsService(
                null!,
                _mockTargetConfig.Object,
                _mockStockConfig.Object,
                _mockEquipmentService.Object);
        }

        [Given(@"there is no equipment")]
        public void GivenThereIsNoEquipment()
        {
            _equipmentList = new List<Equipment>();
            _mockEquipmentService.Setup(s => s.GetAllEquipmentAsync())
                .ReturnsAsync(_equipmentList);

            _mockEquipmentService.Setup(s => s.GetEquipmentParametersAsync())
                .ReturnsAsync((EquipmentParameters?)null);
        }

        [Given(@"there are (.*) pieces of equipment")]
        public void GivenThereArePiecesOfEquipment(int count)
        {
            _equipmentList = new List<Equipment>();
            for (int i = 0; i < count; i++)
                _equipmentList.Add(new Equipment());

            _mockEquipmentService.Setup(s => s.GetAllEquipmentAsync())
                .ReturnsAsync(_equipmentList);
        }

        [Given(@"the equipment parameters are:")]
        public void GivenTheEquipmentParametersAre(Table table)
        {
            _equipmentParameters = new EquipmentParameters
            {
                InputSandKg = int.Parse(table.Rows[0]["InputSandKg"]),
                InputCopperKg = int.Parse(table.Rows[0]["InputCopperKg"]),
                OutputScreens = int.Parse(table.Rows[0]["OutputScreens"])
            };

            _mockEquipmentService.Setup(s => s.GetEquipmentParametersAsync())
                .ReturnsAsync(_equipmentParameters);
        }

        [Given(@"the target quantities are:")]
        public void GivenTheTargetQuantitiesAre(Table table)
        {
            var config = new TargetQuantitiesConfig();
            foreach (var row in table.Rows)
            {
                var material = row["Material"];
                var target = int.Parse(row["Target"]);

                if (material == "Sand")
                    config.Sand.Target = target;
                else if (material == "Copper")
                    config.Copper.Target = target;
            }

            _mockTargetConfig.Setup(c => c.CurrentValue).Returns(config);
        }

        [Given(@"the stock management options have logistics lead time of (.*) days")]
        public void GivenTheStockManagementOptionsHaveLogisticsLeadTimeOfDays(int days)
        {
            _mockStockConfig.Setup(c => c.CurrentValue)
                .Returns(new StockManagementOptions { LogisticsLeadTimeDays = days });
        }

        [When(@"I retrieve the material statistics")]
        public async Task WhenIRetrieveTheMaterialStatistics()
        {
            _statistics = await _service.GetMaterialStatisticsAsync();
        }

        [Then(@"the daily consumption for sand should be (.*)")]
        public void ThenTheDailyConsumptionForSandShouldBe(int expected)
        {
            _statistics.Sand.DailyConsumption.Should().Be(expected);
        }

        [Then(@"the reorder point for sand should be (.*)")]
        public void ThenTheReorderPointForSandShouldBe(int expected)
        {
            _statistics.Sand.ReorderPoint.Should().Be(expected);
        }

        [Then(@"the daily consumption for copper should be (.*)")]
        public void ThenTheDailyConsumptionForCopperShouldBe(int expected)
        {
            _statistics.Copper.DailyConsumption.Should().Be(expected);
        }

        [Then(@"the reorder point for copper should be (.*)")]
        public void ThenTheReorderPointForCopperShouldBe(int expected)
        {
            _statistics.Copper.ReorderPoint.Should().Be(expected);
        }
    }
}
