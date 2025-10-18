using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using ScreenProducerAPI.IntegrationTests.Fixtures;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.ScreenDbContext;
using Microsoft.Extensions.DependencyInjection;
using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.IntegrationTests.Tests.Endpoints;

/// <summary>
/// Integration tests for Product endpoints.
/// Tests the complete request/response pipeline including routing, model binding,
/// business logic, and HTTP response formatting.
/// </summary>
[TestFixture]
public class ProductEndpointTests
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
    public async Task GET_Screens_Returns_Success()
    {
        // Act
        var response = await _client.GetAsync("/screens");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GET_Screens_Returns_ProductResponse_With_Correct_Structure()
    {
        // Act
        var response = await _client.GetAsync("/screens");
        var productResponse = await response.Content.ReadFromJsonAsync<ProductResponse>();

        // Assert
        productResponse.Should().NotBeNull();
        productResponse!.Screens.Should().NotBeNull();
        productResponse.Screens.Quantity.Should().BeGreaterThanOrEqualTo(0);
        productResponse.Screens.Price.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GET_Screens_Returns_Zero_When_No_Products_In_Stock()
    {
        // Arrange - Database starts empty by default

        // Act
        var response = await _client.GetAsync("/screens");
        var productResponse = await response.Content.ReadFromJsonAsync<ProductResponse>();

        // Assert
        productResponse.Should().NotBeNull();
        productResponse!.Screens.Quantity.Should().Be(0);
    }

    [Test]
    public async Task GET_Screens_Returns_Correct_Quantity_When_Products_Exist()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

        var product = new Product
        {
            Price = 100,
            Quantity = 50
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/screens");
        var productResponse = await response.Content.ReadFromJsonAsync<ProductResponse>();

        // Assert
        productResponse.Should().NotBeNull();
        productResponse!.Screens.Quantity.Should().Be(50);
        productResponse.Screens.Price.Should().Be(100);
    }

    [Test]
    public async Task GET_Screens_ContentType_Is_Json()
    {
        // Act
        var response = await _client.GetAsync("/screens");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task GET_Screens_Multiple_Requests_Are_Consistent()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

        var product = new Product
        {
            Price = 100,
            Quantity = 75
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act
        var response1 = await _client.GetAsync("/screens");
        var response2 = await _client.GetAsync("/screens");

        var productResponse1 = await response1.Content.ReadFromJsonAsync<ProductResponse>();
        var productResponse2 = await response2.Content.ReadFromJsonAsync<ProductResponse>();

        // Assert
        productResponse1.Should().NotBeNull();
        productResponse2.Should().NotBeNull();
        productResponse1!.Screens.Quantity.Should().Be(productResponse2!.Screens.Quantity);
        productResponse1.Screens.Price.Should().Be(productResponse2.Screens.Price);
    }
}
