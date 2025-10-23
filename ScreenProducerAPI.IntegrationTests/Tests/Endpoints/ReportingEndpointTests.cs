using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ScreenProducerAPI.IntegrationTests.Fixtures;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;
using System.Net;
using System.Net.Http.Json;

namespace ScreenProducerAPI.IntegrationTests.Tests.Endpoints;

[TestFixture]
public class ReportingEndpointTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GET_Report_HandSimulationStatus_Returns_OK()
    {
        // Act
        var response = await _client.GetAsync("/report/hand-simulation-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GET_Report_HandSimulationStatus_Returns_Valid_Status_Data()
    {
        // Act
        var response = await _client.GetAsync("/report/hand-simulation-status");
        var statusResponse = await response.Content.ReadFromJsonAsync<HandSimulationStatus>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        statusResponse.Should().NotBeNull();
        statusResponse!.EpochStartTime.Should().BeGreaterThan(0);
        // IsRunning and isOnline can be true or false - both are valid states
    }

    [Test]
    public async Task GET_Report_HandSimulationStatus_Content_Type_Is_JSON()
    {
        // Act
        var response = await _client.GetAsync("/report/hand-simulation-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task GET_Report_HandSimulationStatus_Multiple_Calls_Return_Consistent_Data()
    {
        // Act
        var firstResponse = await _client.GetAsync("/report/hand-simulation-status");
        var firstStatus = await firstResponse.Content.ReadFromJsonAsync<HandSimulationStatus>();

        var secondResponse = await _client.GetAsync("/report/hand-simulation-status");
        var secondStatus = await secondResponse.Content.ReadFromJsonAsync<HandSimulationStatus>();

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        firstStatus.Should().NotBeNull();
        secondStatus.Should().NotBeNull();

        // Mock service should return consistent data
        firstStatus!.EpochStartTime.Should().Be(secondStatus!.EpochStartTime);
        firstStatus.IsRunning.Should().Be(secondStatus.IsRunning);
        firstStatus.isOnline.Should().Be(secondStatus.isOnline);
    }

    [Test]
    public async Task GET_Report_DailyReport_Returns_OK_With_Current_Date()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/report");

        // Assert - Should either return OK with data or NotFound if no data exists
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var dailyReport = await response.Content.ReadFromJsonAsync<DailyReport>();
            dailyReport.Should().NotBeNull();
        }
    }

    [Test]
    public async Task GET_Report_DailyReport_With_Specific_Date_Parameter()
    {
        // Arrange
        await SeedTestDataAsync();
        var specificDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/report?date={specificDate}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var dailyReport = await response.Content.ReadFromJsonAsync<DailyReport>();
            dailyReport.Should().NotBeNull();
        }
    }

    [Test]
    public async Task GET_Report_PeriodReports_With_Valid_Days_Returns_OK()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/report/period?pastDaysToInclude=7");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var reports = await response.Content.ReadFromJsonAsync<List<DailyReport>>();
        reports.Should().NotBeNull();
        // Reports can be empty if no data exists, which is valid
    }

    [Test]
    public async Task GET_Report_PeriodReports_With_Invalid_Days_Returns_BadRequest()
    {
        // Act
        var negativeResponse = await _client.GetAsync("/report/period?pastDaysToInclude=-1");
        var zeroResponse = await _client.GetAsync("/report/period?pastDaysToInclude=0");
        var tooLargeResponse = await _client.GetAsync("/report/period?pastDaysToInclude=100");

        // Assert
        negativeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        zeroResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        tooLargeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GET_Report_PeriodReports_Within_Valid_Range_Returns_OK()
    {
        // Arrange
        await SeedTestDataAsync();

        var testCases = new[] { 1, 30, 60, 90 };

        foreach (var days in testCases)
        {
            // Act
            var response = await _client.GetAsync($"/report/period?pastDaysToInclude={days}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Request with {days} days should return OK");

            var reports = await response.Content.ReadFromJsonAsync<List<DailyReport>>();
            reports.Should().NotBeNull();
        }
    }

    [Test]
    public async Task GET_Report_PurchaseOrders_Returns_OK()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/report/purchases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var purchaseOrders = await response.Content.ReadFromJsonAsync<List<PurchaseOrder>>();
        purchaseOrders.Should().NotBeNull();
        // Purchase orders can be empty if no orders exist, which is valid
    }

    [Test]
    public async Task GET_Report_PurchaseOrders_With_Specific_Date_Returns_OK()
    {
        // Arrange
        await SeedTestDataAsync();
        var specificDate = DateTime.Today.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/report/purchases?date={specificDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var purchaseOrders = await response.Content.ReadFromJsonAsync<List<PurchaseOrder>>();
        purchaseOrders.Should().NotBeNull();
    }

    [Test]
    public async Task GET_Report_Endpoints_Accept_Proper_HTTP_Methods()
    {
        // Test that endpoints only accept GET requests
        var endpoints = new[]
        {
            "/report",
            "/report/period?pastDaysToInclude=7",
            "/report/purchases",
            "/report/hand-simulation-status"
        };

        foreach (var endpoint in endpoints)
        {
            // Act - Try POST (should not be allowed)
            var postResponse = await _client.PostAsync(endpoint, null);

            // Assert
            postResponse.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed,
                $"POST should not be allowed for endpoint {endpoint}");
        }
    }

    [Test]
    public async Task GET_Report_Endpoints_Handle_Concurrent_Requests()
    {
        // Arrange
        await SeedTestDataAsync();
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Make multiple concurrent requests to different endpoints
        tasks.Add(_client.GetAsync("/report/hand-simulation-status"));
        tasks.Add(_client.GetAsync("/report/period?pastDaysToInclude=7"));
        tasks.Add(_client.GetAsync("/report/purchases"));
        tasks.Add(_client.GetAsync("/report/hand-simulation-status"));
        tasks.Add(_client.GetAsync("/report/period?pastDaysToInclude=30"));

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(response =>
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        });
    }

    [Test]
    public async Task GET_Report_HandSimulationStatus_Endpoint_Performance()
    {
        // Act & Assert - Multiple rapid calls should complete quickly
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 10; i++)
        {
            var response = await _client.GetAsync("/report/hand-simulation-status");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        stopwatch.Stop();

        // Should complete 10 requests in reasonable time (less than 5 seconds)
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Reporting_Workflow_EndToEnd_Test()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act & Assert - Complete reporting workflow

        // Step 1: Check Hand simulation status
        var statusResponse = await _client.GetAsync("/report/hand-simulation-status");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await statusResponse.Content.ReadFromJsonAsync<HandSimulationStatus>();
        status.Should().NotBeNull();

        // Step 2: Get current daily report
        var dailyReportResponse = await _client.GetAsync("/report");
        dailyReportResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        // Step 3: Get period reports
        var periodResponse = await _client.GetAsync("/report/period?pastDaysToInclude=7");
        periodResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var periodReports = await periodResponse.Content.ReadFromJsonAsync<List<DailyReport>>();
        periodReports.Should().NotBeNull();

        // Step 4: Get purchase orders
        var purchaseResponse = await _client.GetAsync("/report/purchases");
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var purchaseOrders = await purchaseResponse.Content.ReadFromJsonAsync<List<PurchaseOrder>>();
        purchaseOrders.Should().NotBeNull();

        // All requests should have completed successfully
    }

    #region Helper Methods

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

        // Seed order statuses if they don't exist
        if (!await context.OrderStatuses.AnyAsync())
        {
            context.OrderStatuses.AddRange(
                new OrderStatus { Id = 1, Status = "waiting_payment" },
                new OrderStatus { Id = 2, Status = "waiting_delivery" },
                new OrderStatus { Id = 3, Status = "waiting_collection" },
                new OrderStatus { Id = 4, Status = "collected" }
            );
            await context.SaveChangesAsync();
        }

        // Seed basic product data if it doesn't exist
        if (!await context.Products.AnyAsync())
        {
            var product = new Product
            {
                Price = 100,
                Quantity = 1000
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();
        }
    }

    #endregion
}