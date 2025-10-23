using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using ScreenProducerAPI;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;

namespace ScreenProducerAPI.Tests.Integration;

[TestFixture]
public class PaymentEndpointsTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Mock<IScreenOrderService> _mockOrderService = null!;

    [SetUp]
    public void Setup()
    {
        _mockOrderService = new Mock<IScreenOrderService>();

        _factory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IScreenOrderService));
            if (descriptor != null)
                services.Remove(descriptor);
            services.AddSingleton(_mockOrderService.Object);

            // Mock ISimulationTimeProvider
            var simTimeMock = new Mock<ISimulationTimeProvider>();
            services.AddSingleton(simTimeMock.Object);

            var hostedServiceDescriptors = services
                .Where(d => typeof(IHostedService).IsAssignableFrom(d.ServiceType))
                .ToList();
            foreach (var hosted in hostedServiceDescriptors)
                services.Remove(hosted);

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ScreenProducerAPI.ScreenDbContext.ScreenContext));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);
        });
    });

        _client = _factory.CreateClient();
    }


    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task POST_Payment_Returns200_WhenValidNotification()
    {
        // Arrange
        var notification = new TransactionNotification
        {
            TransactionNumber = "TXN123",
            Status = "SUCCESS",
            Amount = 1500,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Description = "42",
            To = "ScreenProducer",
            From = "CommercialBank"
        };

        _mockOrderService
            .Setup(s => s.ProcessPaymentConfirmationAsync(It.IsAny<TransactionNotification>(), "42"))
            .ReturnsAsync(new PaymentConfirmationResponse
            {
                Success = true,
                Message = "Payment processed successfully",
                OrderId = "42",
                AmountReceived = 1500,
                TotalPaid = 1500,
                OrderTotal = 1500,
                RemainingBalance = 0,
                IsFullyPaid = true,
                Status = "Completed",
                ProcessedAt = DateTime.UtcNow
            });

        // Act
        var response = await _client.PostAsJsonAsync("/payment", notification);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Test]
    public async Task POST_Payment_Returns400_WhenNotificationIsInvalid()
    {
        var invalidNotification = new TransactionNotification
        {
            Description = "INVALID",
            Amount = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/payment", invalidNotification);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
