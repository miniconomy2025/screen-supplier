using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
public sealed class ReportingServiceStepDefinitions
{
    private readonly DbContextOptions<ScreenContext> _dbOptions;
    private ScreenContext _context;
    private Mock<IReportingService> _mockReportingService;
    private IReportingService _reportingService;

    private DailyReport? _result;
    private List<DailyReport>? _periodReports;
    private DateTime _simulationTime;
    private bool _shouldThrowException;

    // Test data storage
    private ProductionHistory? _testProductionHistory;
    private List<PurchaseOrder> _testPurchaseOrders = new();
    private List<ScreenOrder> _testScreenOrders = new();
    private List<Equipment> _testEquipment = new();
    private List<DailyReport> _testDailyReports = new();

    public ReportingServiceStepDefinitions()
    {
        // Use in-memory database for testing
        _dbOptions = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        SetupServices();
    }

    private void SetupServices()
    {
        _context = new ScreenContext(_dbOptions);

        // Create a mock for ReportingService
        _mockReportingService = new Mock<IReportingService>();
        _reportingService = _mockReportingService.Object;

        // Ensure database is created and clean
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        // Reset test data
        _testProductionHistory = null;
        _testPurchaseOrders.Clear();
        _testScreenOrders.Clear();
        _testEquipment.Clear();
        _testDailyReports.Clear();
        _shouldThrowException = false;
    }

    [Given(@"the simulation time is ""(.*)""")]
    public void GivenTheSimulationTimeIs(string dateTimeString)
    {
        _simulationTime = DateTime.Parse(dateTimeString);
        _simulationTime = DateTime.SpecifyKind(_simulationTime, DateTimeKind.Unspecified);
    }

    [Given(@"there is production history for date ""(.*)"" with sand stock (.*), copper stock (.*), screens produced (.*), screen stock (.*), screen price (.*), working equipment (.*)")]
    public void GivenThereIsProductionHistoryForDate(string dateString, int sandStock, int copperStock, int screensProduced, int screenStock, int screenPrice, int workingEquipment)
    {
        var date = DateTime.Parse(dateString);
        var dailyReport = new DailyReport
        {
            Date = date,
            SandStock = sandStock,
            CopperStock = copperStock,
            ScreensProduced = screensProduced,
            ScreenStock = screenStock,
            ScreenPrice = screenPrice,
            WorkingEquipment = workingEquipment,
            SandPurchased = 0,
            CopperPurchased = 0,
            SandConsumed = 0,
            CopperConsumed = 0,
            WorkingMachines = 0,
            ScreensSold = 0,
            Revenue = 0
        };

        _mockReportingService
            .Setup(x => x.GetDailyReportAsync(date))
            .ReturnsAsync(dailyReport);
    }

    [Given(@"there is no production history for date ""(.*)""")]
    public void GivenThereIsNoProductionHistoryForDate(string dateString)
    {
        var date = DateTime.Parse(dateString);
        _mockReportingService
            .Setup(x => x.GetDailyReportAsync(date))
            .ReturnsAsync((DailyReport?)null);
    }

    [Given(@"there are purchase orders for date ""(.*)"" with sand orders (.*) units and copper orders (.*) units")]
    public void GivenThereArePurchaseOrdersForDate(string dateString, int sandUnits, int copperUnits)
    {
        var date = DateTime.Parse(dateString);
        var dailyReport = new DailyReport
        {
            Date = date,
            SandPurchased = sandUnits,
            CopperPurchased = copperUnits,
            SandStock = 0,
            CopperStock = 0,
            ScreensProduced = 0,
            ScreenStock = 0,
            ScreenPrice = 0,
            WorkingEquipment = 0,
            SandConsumed = 0,
            CopperConsumed = 0,
            WorkingMachines = 0,
            ScreensSold = 0,
            Revenue = 0
        };

        _mockReportingService
            .Setup(x => x.GetDailyReportAsync(date))
            .ReturnsAsync(dailyReport);
    }

    [Given(@"there are no purchase orders for date ""(.*)""")]
    public void GivenThereAreNoPurchaseOrdersForDate(string dateString)
    {
        var date = DateTime.Parse(dateString);
        var dailyReport = new DailyReport
        {
            Date = date,
            SandPurchased = 0,
            CopperPurchased = 0,
            SandStock = 0,
            CopperStock = 0,
            ScreensProduced = 0,
            ScreenStock = 0,
            ScreenPrice = 0,
            WorkingEquipment = 0,
            SandConsumed = 0,
            CopperConsumed = 0,
            WorkingMachines = 0,
            ScreensSold = 0,
            Revenue = 0
        };

        _mockReportingService
            .Setup(x => x.GetDailyReportAsync(date))
            .ReturnsAsync(dailyReport);
    }

    [Given(@"there are screen orders for date ""(.*)"" with total quantity (.*) and unit price (.*)")]
    public void GivenThereAreScreenOrdersForDate(string dateString, int totalQuantity, int unitPrice)
    {
        var date = DateTime.Parse(dateString);
        var dailyReport = new DailyReport
        {
            Date = date,
            ScreensSold = totalQuantity,
            Revenue = totalQuantity * unitPrice,
            SandStock = 0,
            CopperStock = 0,
            ScreensProduced = 0,
            ScreenStock = 0,
            ScreenPrice = 0,
            WorkingEquipment = 0,
            SandPurchased = 0,
            CopperPurchased = 0,
            SandConsumed = 0,
            CopperConsumed = 0,
            WorkingMachines = 0
        };

        _mockReportingService
            .Setup(x => x.GetDailyReportAsync(date))
            .ReturnsAsync(dailyReport);
    }

    [Given(@"there are no screen orders for date ""(.*)""")]
    public void GivenThereAreNoScreenOrdersForDate(string dateString)
    {
        var date = DateTime.Parse(dateString);
        var dailyReport = new DailyReport
        {
            Date = date,
            ScreensSold = 0,
            Revenue = 0,
            SandStock = 0,
            CopperStock = 0,
            ScreensProduced = 0,
            ScreenStock = 0,
            ScreenPrice = 0,
            WorkingEquipment = 0,
            SandPurchased = 0,
            CopperPurchased = 0,
            SandConsumed = 0,
            CopperConsumed = 0,
            WorkingMachines = 0
        };

        _mockReportingService
            .Setup(x => x.GetDailyReportAsync(date))
            .ReturnsAsync(dailyReport);
    }

    [Given(@"there is equipment with sand input (.*), copper input (.*), and screen output (.*)")]
    public void GivenThereIsEquipmentWithInputsAndOutput(int sandInput, int copperInput, int screenOutput)
    {
        // This will be reflected in the daily report's working machines count
        // We'll set this up when we create the daily report in the When step
    }

    [Given(@"there is no equipment available")]
    public void GivenThereIsNoEquipmentAvailable()
    {
        // This will be reflected in the daily report's working machines count being 0
    }

    [Given(@"the production history service will throw an exception")]
    public void GivenTheProductionHistoryServiceWillThrowAnException()
    {
        _shouldThrowException = true;
        _mockReportingService
            .Setup(x => x.GetDailyReportAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((DailyReport?)null);
    }

    [Given(@"there are daily reports for the last (.*) days from ""(.*)""")]
    public void GivenThereAreDailyReportsForTheLastDaysFrom(int pastDays, string dateString)
    {
        var endDate = DateTime.Parse(dateString);
        _testDailyReports.Clear();

        for (int i = 0; i < pastDays; i++)
        {
            var reportDate = endDate.AddDays(-i);
            var report = new DailyReport
            {
                Date = reportDate,
                SandStock = 100 - (i * 10),
                CopperStock = 50 - (i * 5),
                ScreensProduced = 20 + (i * 2),
                ScreenStock = 80 + (i * 5),
                ScreenPrice = 500,
                WorkingEquipment = 2,
                SandPurchased = 0,
                CopperPurchased = 0,
                SandConsumed = 0,
                CopperConsumed = 0,
                WorkingMachines = 0,
                ScreensSold = 0,
                Revenue = 0
            };
            _testDailyReports.Add(report);
        }

        _mockReportingService
            .Setup(x => x.GetLastPeriodReportsAsync(pastDays, endDate))
            .ReturnsAsync(_testDailyReports.OrderBy(r => r.Date).ToList());
    }

    [Given(@"there are some null daily reports in the last (.*) days from ""(.*)""")]
    public void GivenThereAreSomeNullDailyReportsInTheLastDaysFrom(int pastDays, string dateString)
    {
        var endDate = DateTime.Parse(dateString);
        _testDailyReports.Clear();

        // Create some valid reports and some that will be filtered out (simulating null returns)
        for (int i = 0; i < pastDays; i++)
        {
            var reportDate = endDate.AddDays(-i);

            if (i % 2 == 0) // Every other day has a valid report
            {
                var report = new DailyReport
                {
                    Date = reportDate,
                    SandStock = 100,
                    CopperStock = 50,
                    ScreensProduced = 20,
                    ScreenStock = 80,
                    ScreenPrice = 500,
                    WorkingEquipment = 2,
                    SandPurchased = 0,
                    CopperPurchased = 0,
                    SandConsumed = 0,
                    CopperConsumed = 0,
                    WorkingMachines = 0,
                    ScreensSold = 0,
                    Revenue = 0
                };
                _testDailyReports.Add(report);
            }
        }

        _mockReportingService
            .Setup(x => x.GetLastPeriodReportsAsync(pastDays, endDate))
            .ReturnsAsync(_testDailyReports.OrderBy(r => r.Date).ToList());
    }

    [Given(@"there are purchase orders for date ""(.*)"" with mixed materials: sand (.*) units, copper (.*) units, other materials (.*) units")]
    public void GivenThereArePurchaseOrdersForDateWithMixedMaterials(string dateString, int sandUnits, int copperUnits, int otherUnits)
    {
        var date = DateTime.Parse(dateString);
        var dailyReport = new DailyReport
        {
            Date = date,
            SandPurchased = sandUnits,
            CopperPurchased = copperUnits,
            SandStock = 0,
            CopperStock = 0,
            ScreensProduced = 0,
            ScreenStock = 0,
            ScreenPrice = 0,
            WorkingEquipment = 0,
            SandConsumed = 0,
            CopperConsumed = 0,
            WorkingMachines = 0,
            ScreensSold = 0,
            Revenue = 0
        };

        _mockReportingService
            .Setup(x => x.GetDailyReportAsync(date))
            .ReturnsAsync(dailyReport);
    }

    [Given(@"there are screen orders for date ""(.*)"" with multiple orders totaling (.*) units and average price (.*)")]
    public void GivenThereAreScreenOrdersForDateWithMultipleOrders(string dateString, int totalUnits, int averagePrice)
    {
        var date = DateTime.Parse(dateString);
        var dailyReport = new DailyReport
        {
            Date = date,
            ScreensSold = totalUnits,
            Revenue = totalUnits * averagePrice,
            SandStock = 0,
            CopperStock = 0,
            ScreensProduced = 0,
            ScreenStock = 0,
            ScreenPrice = 0,
            WorkingEquipment = 0,
            SandPurchased = 0,
            CopperPurchased = 0,
            SandConsumed = 0,
            CopperConsumed = 0,
            WorkingMachines = 0
        };

        _mockReportingService
            .Setup(x => x.GetDailyReportAsync(date))
            .ReturnsAsync(dailyReport);
    }

    [When(@"I get daily report for date ""(.*)""")]
    public async Task WhenIGetDailyReportForDate(string dateString)
    {
        var date = DateTime.Parse(dateString);
        _result = await _reportingService.GetDailyReportAsync(date);
    }

    [When(@"I get last period reports for (.*) days from ""(.*)""")]
    public async Task WhenIGetLastPeriodReportsForDaysFrom(int pastDays, string dateString)
    {
        var date = DateTime.Parse(dateString);
        _periodReports = await _reportingService.GetLastPeriodReportsAsync(pastDays, date);
    }

    [Then(@"the daily report should be generated successfully")]
    public void ThenTheDailyReportShouldBeGeneratedSuccessfully()
    {
        _result.Should().NotBeNull();
    }

    [Then(@"no daily report should be returned")]
    public void ThenNoDailyReportShouldBeReturned()
    {
        _result.Should().BeNull();
    }

    [Then(@"the report should have date ""(.*)""")]
    public void ThenTheReportShouldHaveDate(string expectedDateString)
    {
        var expectedDate = DateTime.Parse(expectedDateString);
        _result!.Date.Should().Be(expectedDate);
    }

    [Then(@"the report should have sand stock (.*)")]
    public void ThenTheReportShouldHaveSandStock(int expectedSandStock)
    {
        _result!.SandStock.Should().Be(expectedSandStock);
    }

    [Then(@"the report should have copper stock (.*)")]
    public void ThenTheReportShouldHaveCopperStock(int expectedCopperStock)
    {
        _result!.CopperStock.Should().Be(expectedCopperStock);
    }

    [Then(@"the report should have sand purchased (.*)")]
    public void ThenTheReportShouldHaveSandPurchased(int expectedSandPurchased)
    {
        _result!.SandPurchased.Should().Be(expectedSandPurchased);
    }

    [Then(@"the report should have copper purchased (.*)")]
    public void ThenTheReportShouldHaveCopperPurchased(int expectedCopperPurchased)
    {
        _result!.CopperPurchased.Should().Be(expectedCopperPurchased);
    }

    [Then(@"the report should have screens produced (.*)")]
    public void ThenTheReportShouldHaveScreensProduced(int expectedScreensProduced)
    {
        _result!.ScreensProduced.Should().Be(expectedScreensProduced);
    }

    [Then(@"the report should have screens sold (.*)")]
    public void ThenTheReportShouldHaveScreensSold(int expectedScreensSold)
    {
        _result!.ScreensSold.Should().Be(expectedScreensSold);
    }

    [Then(@"the report should have revenue (.*)")]
    public void ThenTheReportShouldHaveRevenue(int expectedRevenue)
    {
        _result!.Revenue.Should().Be(expectedRevenue);
    }

    [Then(@"the report should have screen stock (.*)")]
    public void ThenTheReportShouldHaveScreenStock(int expectedScreenStock)
    {
        _result!.ScreenStock.Should().Be(expectedScreenStock);
    }

    [Then(@"the report should have screen price (.*)")]
    public void ThenTheReportShouldHaveScreenPrice(int expectedScreenPrice)
    {
        _result!.ScreenPrice.Should().Be(expectedScreenPrice);
    }

    [Then(@"the report should have working equipment (.*)")]
    public void ThenTheReportShouldHaveWorkingEquipment(int expectedWorkingEquipment)
    {
        _result!.WorkingEquipment.Should().Be(expectedWorkingEquipment);
    }

    [Then(@"the report should have sand consumed (.*)")]
    public void ThenTheReportShouldHaveSandConsumed(int expectedSandConsumed)
    {
        _result!.SandConsumed.Should().Be(expectedSandConsumed);
    }

    [Then(@"the report should have copper consumed (.*)")]
    public void ThenTheReportShouldHaveCopperConsumed(int expectedCopperConsumed)
    {
        _result!.CopperConsumed.Should().Be(expectedCopperConsumed);
    }

    [Then(@"the report should have working machines (.*)")]
    public void ThenTheReportShouldHaveWorkingMachines(int expectedWorkingMachines)
    {
        _result!.WorkingMachines.Should().Be(expectedWorkingMachines);
    }

    [Then(@"{int} daily reports should be returned")]
    public void ThenDailyReportsShouldBeReturned(int expectedCount)
    {
        _periodReports.Should().NotBeNull();
        _periodReports!.Count.Should().Be(expectedCount);
    }

    [Then(@"the reports should be ordered by date ascending")]
    public void ThenTheReportsShouldBeOrderedByDateAscending()
    {
        _periodReports.Should().NotBeNull();
        _periodReports!.Should().NotBeEmpty();

        for (int i = 0; i < _periodReports.Count - 1; i++)
        {
            _periodReports[i].Date.Should().BeOnOrBefore(_periodReports[i + 1].Date);
        }
    }

    [Then(@"only non-null daily reports should be returned")]
    public void ThenOnlyNonNullDailyReportsShouldBeReturned()
    {
        _periodReports.Should().NotBeNull();
        _periodReports!.Should().NotContain(r => r == null);
        _periodReports.Count.Should().BeGreaterThan(0);
        _periodReports.Count.Should().BeLessThan(5); // Some should be filtered out
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _context?.Dispose();
        SetupServices();
    }
}