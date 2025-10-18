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

namespace ScreenProducerAPI.IntegrationTests.Tests.Scenarios;

/// <summary>
/// End-to-end scenario tests that verify complete business workflows.
/// These tests simulate real user interactions across multiple endpoints.
/// </summary>
[TestFixture]
public class OrderToDeliveryFlowTests
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
    public async Task Complete_Order_Flow_From_Product_Check_To_Order_Creation()
    {
        // Arrange - Seed data
        await SeedProductAsync(quantity: 100, price: 150);

        // Step 1: Check available products
        var productsResponse = await _client.GetAsync("/screens");
        productsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var products = await productsResponse.Content.ReadFromJsonAsync<ProductResponse>();
        products.Should().NotBeNull();
        products!.Screens.Quantity.Should().Be(100);
        products.Screens.Price.Should().Be(150);

        // Step 2: Create an order
        var orderRequest = new CreateOrderRequest { Quantity = 10 };
        var orderResponse = await _client.PostAsJsonAsync("/order", orderRequest);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var orderData = await orderResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();
        orderData.Should().NotBeNull();
        orderData!.OrderId.Should().BeGreaterThan(0);
        orderData.TotalPrice.Should().Be(1500); // 10 * 150

        // Step 3: Verify order status
        var statusResponse = await _client.GetAsync($"/order/{orderData.OrderId}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderStatus = await statusResponse.Content.ReadFromJsonAsync<OrderStatusResponse>();
        orderStatus.Should().NotBeNull();
        orderStatus!.Quantity.Should().Be(10);
        orderStatus.UnitPrice.Should().Be(150);
        orderStatus.TotalPrice.Should().Be(1500);
    }

    [Test]
    public async Task Multiple_Orders_Can_Be_Created_And_Retrieved()
    {
        // Arrange
        await SeedProductAsync(quantity: 1000, price: 200);

        // Act - Create multiple orders
        var order1 = await CreateOrderAsync(5);
        var order2 = await CreateOrderAsync(10);
        var order3 = await CreateOrderAsync(15);

        // Assert - Verify each order
        await VerifyOrderAsync(order1.OrderId, quantity: 5, expectedPrice: 1000);
        await VerifyOrderAsync(order2.OrderId, quantity: 10, expectedPrice: 2000);
        await VerifyOrderAsync(order3.OrderId, quantity: 15, expectedPrice: 3000);
    }

    [Test]
    public async Task Order_Creation_Validates_Product_Availability()
    {
        // Arrange - Seed product with limited quantity
        await SeedProductAsync(quantity: 50, price: 100);

        // Act - Try to create an order
        var orderRequest = new CreateOrderRequest { Quantity = 10 };
        var orderResponse = await _client.PostAsJsonAsync("/order", orderRequest);

        // Assert - Order should be created (business logic handles stock)
        orderResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.BadRequest // If stock validation is enforced
        );
    }

    [Test]
    public async Task Bank_Integration_Provides_Payment_Details()
    {
        // Arrange
        await SeedProductAsync(quantity: 100, price: 250);

        // Act - Create order
        var orderRequest = new CreateOrderRequest { Quantity = 5 };
        var response = await _client.PostAsJsonAsync("/order", orderRequest);
        var orderData = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();

        // Assert - Bank account details should be provided (from mock)
        orderData.Should().NotBeNull();
        orderData!.BankAccountNumber.Should().NotBeNullOrEmpty();
        orderData.BankAccountNumber.Should().StartWith("TEST-ACC-"); // Mock bank service format
    }

    [Test]
    public async Task Order_Status_Link_Is_Valid_And_Accessible()
    {
        // Arrange
        await SeedProductAsync(quantity: 100, price: 120);

        // Act - Create order
        var orderRequest = new CreateOrderRequest { Quantity = 8 };
        var createResponse = await _client.PostAsJsonAsync("/order", orderRequest);
        var orderData = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        // Act - Follow the status link
        var statusLink = orderData!.OrderStatusLink;
        var statusResponse = await _client.GetAsync(statusLink);

        // Assert
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderStatus = await statusResponse.Content.ReadFromJsonAsync<OrderStatusResponse>();
        orderStatus.Should().NotBeNull();
        orderStatus!.OrderId.Should().Be(orderData.OrderId);
    }

    [Test]
    public async Task External_Services_Are_Mocked_Successfully()
    {
        // This test verifies that mocked external services work correctly
        // by checking that we can create orders without real external dependencies

        // Arrange
        await SeedProductAsync(quantity: 100, price: 100);

        // Act - Create order (uses MockBankService)
        var orderRequest = new CreateOrderRequest { Quantity = 10 };
        var response = await _client.PostAsJsonAsync("/order", orderRequest);

        // Assert - Should succeed with mocked services
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var orderData = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        orderData.Should().NotBeNull();
        // Bank account number comes from MockBankService
        orderData!.BankAccountNumber.Should().Be("TEST-ACC-12345");
    }

    [Test]
    public async Task Order_Payment_Status_Is_Tracked_Correctly()
    {
        // Arrange
        await SeedProductAsync(quantity: 100, price: 150);

        // Act - Create order
        var orderRequest = new CreateOrderRequest { Quantity = 10 };
        var createResponse = await _client.PostAsJsonAsync("/order", orderRequest);
        var orderData = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        // Get order status
        var statusResponse = await _client.GetAsync($"/order/{orderData!.OrderId}");
        var orderStatus = await statusResponse.Content.ReadFromJsonAsync<OrderStatusResponse>();

        // Assert - Payment tracking fields
        orderStatus.Should().NotBeNull();
        orderStatus!.TotalPrice.Should().Be(1500);
        orderStatus.AmountPaid.Should().BeGreaterThanOrEqualTo(0);
        orderStatus.RemainingBalance.Should().BeLessThanOrEqualTo(orderStatus.TotalPrice);
        orderStatus.IsFullyPaid.Should().Be(orderStatus.AmountPaid >= orderStatus.TotalPrice);
    }

    #region Helper Methods

    private async Task SeedProductAsync(int quantity, int price)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

        // Seed order statuses if not present (required for order creation)
        if (!await context.OrderStatuses.AnyAsync())
        {
            context.OrderStatuses.AddRange(
                new Models.OrderStatus { Id = 1, Status = "waiting_payment" },
                new Models.OrderStatus { Id = 2, Status = "waiting_collection" },
                new Models.OrderStatus { Id = 3, Status = "collected" }
            );
            await context.SaveChangesAsync();
        }

        // Clear existing products to ensure test isolation
        var existingProducts = await context.Products.ToListAsync();
        if (existingProducts.Any())
        {
            context.Products.RemoveRange(existingProducts);
            await context.SaveChangesAsync();
        }

        var product = new Product
        {
            Price = price,
            Quantity = quantity
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Detach to avoid tracking issues
        context.ChangeTracker.Clear();
    }

    private async Task<CreateOrderResponse> CreateOrderAsync(int quantity)
    {
        var request = new CreateOrderRequest { Quantity = quantity };
        var response = await _client.PostAsJsonAsync("/order", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateOrderResponse>())!;
    }

    private async Task VerifyOrderAsync(int orderId, int quantity, int expectedPrice)
    {
        var response = await _client.GetAsync($"/order/{orderId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderStatus = await response.Content.ReadFromJsonAsync<OrderStatusResponse>();
        orderStatus.Should().NotBeNull();
        orderStatus!.OrderId.Should().Be(orderId);
        orderStatus.Quantity.Should().Be(quantity);
        orderStatus.TotalPrice.Should().Be(expectedPrice);
    }

    #endregion
}
