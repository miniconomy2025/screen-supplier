using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ScreenProducerAPI.IntegrationTests.Fixtures;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.ScreenDbContext;
using System.Net;
using System.Net.Http.Json;

namespace ScreenProducerAPI.IntegrationTests.Tests.Endpoints;

[TestFixture]
public class LogisticsEndpointTests
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
    public async Task POST_Logistics_With_Valid_Delivery_Request_Returns_OK()
    {
        // Arrange
        await SeedTestDataAsync();

        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 100,
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task POST_Logistics_Delivery_Returns_Correct_Response_Data()
    {
        // Arrange
        await SeedTestDataAsync();

        var request = new LogisticsRequest
        {
            Id = 2,
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "copper",
                    Quantity = 50,
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);
        var logisticsResponse = await response.Content.ReadFromJsonAsync<LogisticsResponse>();

        // Assert
        logisticsResponse.Should().NotBeNull();
        logisticsResponse!.Success.Should().BeTrue();
        logisticsResponse.Id.Should().Be(2);
        logisticsResponse.OrderId.Should().Be(1002); // Mock adds 1000
        logisticsResponse.Quantity.Should().Be(50);
        logisticsResponse.ItemType.Should().Be("sand"); // Mock default
        logisticsResponse.Message.Should().Contain("Successfully received 50 units");
        logisticsResponse.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task POST_Logistics_With_Valid_Pickup_Request_Returns_OK()
    {
        // Arrange
        await SeedTestDataAsync();

        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "PICKUP",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "screens",
                    Quantity = 10,
                    MeasurementType = "UNIT"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task POST_Logistics_Pickup_Returns_Correct_Response_Data()
    {
        // Arrange
        await SeedTestDataAsync();

        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "PICKUP",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "screens",
                    Quantity = 15,
                    MeasurementType = "UNIT"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);
        var logisticsResponse = await response.Content.ReadFromJsonAsync<LogisticsResponse>();

        // Assert
        logisticsResponse.Should().NotBeNull();
        logisticsResponse!.Success.Should().BeTrue();
        logisticsResponse.OrderId.Should().Be(1);
        logisticsResponse.Quantity.Should().Be(15);
        logisticsResponse.ItemType.Should().Be("screens");
        logisticsResponse.Message.Should().Be("collected");
        logisticsResponse.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task POST_Logistics_With_Invalid_Id_Returns_BadRequest()
    {
        // Arrange
        var request = new LogisticsRequest
        {
            Id = 0, // Invalid ID
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 100,
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_With_Negative_Id_Returns_BadRequest()
    {
        // Arrange
        var request = new LogisticsRequest
        {
            Id = -1, // Negative ID
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 100,
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_With_Invalid_Quantity_Returns_BadRequest()
    {
        // Arrange
        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 0, // Invalid quantity
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_With_Negative_Quantity_Returns_BadRequest()
    {
        // Arrange
        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = -5, // Negative quantity
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_With_Empty_Type_Returns_BadRequest()
    {
        // Arrange
        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "", // Empty type
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 100,
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_With_Null_Type_Returns_BadRequest()
    {
        // Arrange
        var request = new LogisticsRequest
        {
            Id = 1,
            Type = null!, // Null type
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 100,
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_With_Whitespace_Type_Returns_BadRequest()
    {
        // Arrange
        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "   ", // Whitespace type
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 100,
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_With_Invalid_Type_Returns_BadRequest()
    {
        // Arrange
        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "INVALID_TYPE",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 100,
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_With_Null_Request_Returns_BadRequest()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/logistics", (LogisticsRequest?)null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_With_Null_Items_Returns_BadRequest()
    {
        // Arrange
        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "DELIVERY",
            Items = null! // Null items
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_With_Empty_Items_Returns_BadRequest()
    {
        // Arrange
        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>() // Empty items list
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task POST_Logistics_Pickup_For_NonExistent_Order_Returns_NotFound()
    {
        // Arrange
        await SeedTestDataAsync();

        var request = new LogisticsRequest
        {
            Id = 99999, // This ID will return null from mock
            Type = "PICKUP",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "screens",
                    Quantity = 10,
                    MeasurementType = "UNIT"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task POST_Logistics_Case_Sensitivity_Test()
    {
        // Arrange - Test that the endpoint accepts both upper and lower case types
        await SeedTestDataAsync();

        var testCases = new[]
        {
            ("delivery", "Delivery type should be case insensitive"),
            ("DELIVERY", "DELIVERY type should work"),
            ("pickup", "Pickup type should be case insensitive"),
            ("PICKUP", "PICKUP type should work"),
            ("Delivery", "Mixed case Delivery should work"),
            ("Pickup", "Mixed case Pickup should work")
        };

        foreach (var (typeValue, description) in testCases)
        {
            var request = new LogisticsRequest
            {
                Id = 1,
                Type = typeValue,
                Items = new List<PickupRequestItem>
                {
                    new PickupRequestItem
                    {
                        ItemName = "sand",
                        Quantity = 100,
                        MeasurementType = "KG"
                    }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/logistics", request);

            // Assert - Should not return BadRequest due to case sensitivity
            response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest, description);
        }
    }

    [Test]
    public async Task POST_Logistics_Request_Structure_Validation()
    {
        // Test to ensure the endpoint accepts properly structured requests
        // Even if the actual logistics operation fails due to missing data

        var validDeliveryRequest = new LogisticsRequest
        {
            Id = 123,
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 50,
                    MeasurementType = "KG"
                }
            }
        };

        var validPickupRequest = new LogisticsRequest
        {
            Id = 456,
            Type = "PICKUP",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "screens",
                    Quantity = 10,
                    MeasurementType = "UNIT"
                }
            }
        };

        // Act
        var deliveryResponse = await _client.PostAsJsonAsync("/logistics", validDeliveryRequest);
        var pickupResponse = await _client.PostAsJsonAsync("/logistics", validPickupRequest);

        // Assert - Should pass basic validation (not return BadRequest for structure)
        deliveryResponse.StatusCode.Should().NotBe(HttpStatusCode.BadRequest,
            "Valid delivery request structure should pass validation");
        pickupResponse.StatusCode.Should().NotBe(HttpStatusCode.BadRequest,
            "Valid pickup request structure should pass validation");
    }

    [Test]
    public async Task POST_Logistics_Multiple_Items_Handling()
    {
        // Test that the endpoint handles requests with multiple items
        // The current implementation only uses the first item, but the structure should be valid

        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 100,
                    MeasurementType = "KG"
                },
                new PickupRequestItem
                {
                    ItemName = "copper",
                    Quantity = 50,
                    MeasurementType = "KG"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert - Should not fail validation for having multiple items
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest,
            "Request with multiple items should pass basic validation");
    }

    [Test]
    public async Task POST_Logistics_Endpoint_Content_Type_Validation()
    {
        // Test that the endpoint properly handles JSON content
        var request = new LogisticsRequest
        {
            Id = 1,
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 100,
                    MeasurementType = "KG"
                }
            }
        };

        // Act - Send with proper JSON content type
        var response = await _client.PostAsJsonAsync("/logistics", request);

        // Assert - Should accept JSON content (not return UnsupportedMediaType)
        response.StatusCode.Should().NotBe(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task Logistics_Workflow_EndToEnd_Delivery_Test()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act 1: Create a delivery request
        var deliveryRequest = new LogisticsRequest
        {
            Id = 1,
            Type = "DELIVERY",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "sand",
                    Quantity = 200,
                    MeasurementType = "KG"
                }
            }
        };

        var deliveryResponse = await _client.PostAsJsonAsync("/logistics", deliveryRequest);
        var deliveryResult = await deliveryResponse.Content.ReadFromJsonAsync<LogisticsResponse>();

        // Assert
        deliveryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deliveryResult.Should().NotBeNull();
        deliveryResult!.Success.Should().BeTrue();
        deliveryResult.Quantity.Should().Be(200);
        deliveryResult.ItemType.Should().NotBeNullOrEmpty();
        deliveryResult.Message.Should().Contain("Successfully received");
    }

    [Test]
    public async Task Logistics_Workflow_EndToEnd_Pickup_Test()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act 1: Create a pickup request
        var pickupRequest = new LogisticsRequest
        {
            Id = 1,
            Type = "PICKUP",
            Items = new List<PickupRequestItem>
            {
                new PickupRequestItem
                {
                    ItemName = "screens",
                    Quantity = 25,
                    MeasurementType = "UNIT"
                }
            }
        };

        var pickupResponse = await _client.PostAsJsonAsync("/logistics", pickupRequest);
        var pickupResult = await pickupResponse.Content.ReadFromJsonAsync<LogisticsResponse>();

        // Assert
        pickupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        pickupResult.Should().NotBeNull();
        pickupResult!.Success.Should().BeTrue();
        pickupResult.Quantity.Should().Be(25);
        pickupResult.ItemType.Should().Be("screens");
        pickupResult.Message.Should().Be("collected");
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