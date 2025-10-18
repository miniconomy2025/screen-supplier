using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using ScreenProducerAPI.IntegrationTests.Fixtures;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.ScreenDbContext;
using Microsoft.Extensions.DependencyInjection;
using ScreenProducerAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ScreenProducerAPI.IntegrationTests.Tests.Endpoints;

/// Integration tests for Order endpoints.
/// Tests the complete order creation and retrieval workflow.
[TestFixture]
public class OrderEndpointTests
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
    public async Task POST_Order_With_Valid_Request_Returns_Created()
    {
        // Arrange
        await SeedProductAsync();

        var request = new CreateOrderRequest
        {
            Quantity = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/order", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task POST_Order_Returns_CreateOrderResponse_With_Correct_Data()
    {
        // Arrange
        await SeedProductAsync();

        var request = new CreateOrderRequest
        {
            Quantity = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/order", request);
        var orderResponse = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();

        // Assert
        orderResponse.Should().NotBeNull();
        orderResponse!.OrderId.Should().BeGreaterThan(0);
        orderResponse.TotalPrice.Should().BeGreaterThan(0);
        orderResponse.BankAccountNumber.Should().NotBeNullOrEmpty();
        orderResponse.OrderStatusLink.Should().Contain("/order/");
    }

    [Test]
    public async Task POST_Order_With_Zero_Quantity_Returns_BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            Quantity = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/order", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Order_With_Negative_Quantity_Returns_BadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            Quantity = -5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/order", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GET_Order_Status_Returns_Success_For_Existing_Order()
    {
        // Arrange
        await SeedProductAsync();
        var orderId = await CreateTestOrderAsync(10);

        // Act
        var response = await _client.GetAsync($"/order/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GET_Order_Status_Returns_Correct_Order_Details()
    {
        // Arrange
        await SeedProductAsync();
        var orderId = await CreateTestOrderAsync(10);

        // Act
        var response = await _client.GetAsync($"/order/{orderId}");
        var orderStatus = await response.Content.ReadFromJsonAsync<OrderStatusResponse>();

        // Assert
        orderStatus.Should().NotBeNull();
        orderStatus!.OrderId.Should().Be(orderId);
        orderStatus.Quantity.Should().Be(10);
        orderStatus.UnitPrice.Should().BeGreaterThan(0);
        orderStatus.TotalPrice.Should().Be(orderStatus.Quantity * orderStatus.UnitPrice);
        orderStatus.Status.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task GET_Order_Status_For_NonExistent_Order_Returns_NotFound()
    {
        // Act
        var response = await _client.GetAsync("/order/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GET_LastPeriodOrders_With_Valid_Days_Returns_Success()
    {
        // Arrange
        await SeedProductAsync();
        await CreateTestOrderAsync(5);
        await CreateTestOrderAsync(10);

        // Act
        var response = await _client.GetAsync("/order/period?pastDaysToInclude=7");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

    }

    [Test]
    public async Task GET_LastPeriodOrders_With_Invalid_Days_Returns_BadRequest()
    {
        // Act - Testing with 0 days
        var response1 = await _client.GetAsync("/order/period?pastDaysToInclude=0");

        // Act - Testing with > 90 days
        var response2 = await _client.GetAsync("/order/period?pastDaysToInclude=100");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Order_Creates_Order_In_Database()
    {
        // Arrange
        await SeedProductAsync();
        var request = new CreateOrderRequest { Quantity = 15 };

        // Act
        var response = await _client.PostAsJsonAsync("/order", request);
        var orderResponse = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();

        // Assert - Verify in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();
        var order = await context.ScreenOrders.FindAsync(orderResponse!.OrderId);

        order.Should().NotBeNull();
        order!.Quantity.Should().Be(15);
    }

    [Test]
    public async Task Order_Workflow_EndToEnd_Test()
    {
        // Arrange
        await SeedProductAsync();

        // Act 1: Create an order
        var createRequest = new CreateOrderRequest { Quantity = 20 };
        var createResponse = await _client.PostAsJsonAsync("/order", createRequest);
        var orderResponse = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        // Act 2: Get order status
        var statusResponse = await _client.GetAsync($"/order/{orderResponse!.OrderId}");
        var orderStatus = await statusResponse.Content.ReadFromJsonAsync<OrderStatusResponse>();

        // Assert
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        orderStatus.Should().NotBeNull();
        orderStatus!.OrderId.Should().Be(orderResponse.OrderId);
        orderStatus.Quantity.Should().Be(20);
        orderStatus.TotalPrice.Should().Be(orderResponse.TotalPrice);
    }

    #region Helper Methods

    private async Task SeedProductAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

        if (!await context.OrderStatuses.AnyAsync())
        {
            context.OrderStatuses.AddRange(
                new Models.OrderStatus { Id = 1, Status = "waiting_payment" },
                new Models.OrderStatus { Id = 2, Status = "waiting_collection" },
                new Models.OrderStatus { Id = 3, Status = "collected" }
            );
            await context.SaveChangesAsync();
        }

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

    private async Task<int> CreateTestOrderAsync(int quantity)
    {
        var request = new CreateOrderRequest { Quantity = quantity };
        var response = await _client.PostAsJsonAsync("/order", request);
        var orderResponse = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        return orderResponse!.OrderId;
    }

    #endregion
}
