using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ScreenProducerAPI.IntegrationTests.Fixtures;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.IntegrationTests.Tests.Endpoints;

[TestFixture]
public class TargetQuantityEndpointTests
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
    public async Task GET_Targets_Returns_Success()
    {
        // Act
        var response = await _client.GetAsync("/targets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GET_Targets_Returns_InventoryStatus_With_Correct_Structure()
    {
        // Act
        var response = await _client.GetAsync("/targets");
        var result = await response.Content.ReadFromJsonAsync<InventoryStatus>();

        // Assert
        result.Should().NotBeNull();
        result!.Sand.Should().NotBeNull();
        result.Copper.Should().NotBeNull();
        result.Equipment.Should().NotBeNull();
    }

    [Test]
    public async Task GET_Targets_Returns_Zero_When_No_Data_Exists()
    {
        // Act
        var response = await _client.GetAsync("/targets");
        var result = await response.Content.ReadFromJsonAsync<InventoryStatus>();

        // Assert
        result.Should().NotBeNull();
        result!.Sand.Total.Should().Be(0);
        result.Copper.Total.Should().Be(0);
        result.Equipment.Total.Should().Be(0);
    }

    [Test]
    public async Task GET_Targets_Returns_Correct_Totals_When_Data_Exists()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

        var sand = new Material { Name = "Sand", Quantity = 50 };
        var copper = new Material { Name = "Copper", Quantity = 150 };
        context.Materials.AddRange(sand, copper);

        var equipment = new Equipment { ParametersID=1, IsAvailable = true };
        context.Equipment.Add(equipment);

        var status = new OrderStatus { Status = Status.RequiresDelivery };
        context.OrderStatuses.Add(status);

        context.PurchaseOrders.AddRange(
            new PurchaseOrder
            {
                RawMaterial = sand,
                Quantity = 20,
                QuantityDelivered = 5,
                OrderStatus = status,
                EquipmentOrder = false,
                BankAccountNumber = "1234567890",
                Origin = "Screen Supplier"
            },
            new PurchaseOrder
            {
                EquipmentOrder = true,
                Quantity = 5,
                QuantityDelivered = 2,
                OrderStatus = status,
                BankAccountNumber = "1234567890",
                Origin = "Screen Supplier"
            }
        );

        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/targets");
        var result = await response.Content.ReadFromJsonAsync<InventoryStatus>();

        // Assert
        result.Should().NotBeNull();

        // Sand: 50 current + (20 - 5) incoming
        result!.Sand.Current.Should().Be(50);
        result.Sand.Incoming.Should().Be(15);
        result.Sand.Total.Should().Be(65);

        // Copper: 150 current, no incoming
        result.Copper.Current.Should().Be(150);
        result.Copper.Incoming.Should().Be(0);

        // Equipment: 1 available + (5 - 2) incoming
        result.Equipment.Current.Should().Be(1);
        result.Equipment.Incoming.Should().Be(3);
        result.Equipment.Total.Should().Be(4);
    }

    [Test]
    public async Task GET_Targets_ContentType_Is_Json()
    {
        // Act
        var response = await _client.GetAsync("/targets");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task GET_Targets_Multiple_Requests_Are_Consistent()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

        var sand = new Material { Name = "Sand", Quantity = 100 };
        context.Materials.Add(sand);
        await context.SaveChangesAsync();

        // Act
        var response1 = await _client.GetAsync("/targets");
        var response2 = await _client.GetAsync("/targets");

        var result1 = await response1.Content.ReadFromJsonAsync<InventoryStatus>();
        var result2 = await response2.Content.ReadFromJsonAsync<InventoryStatus>();

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.Sand.Total.Should().Be(result2!.Sand.Total);
        result1.Copper.Total.Should().Be(result2.Copper.Total);
    }
}
