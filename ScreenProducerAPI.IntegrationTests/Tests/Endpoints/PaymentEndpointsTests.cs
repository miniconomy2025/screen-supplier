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
            // Remove the real IScreenOrderService and replace with mock
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IScreenOrderService));
            if (descriptor != null)
                services.Remove(descriptor);
            services.AddSingleton(_mockOrderService.Object);

            // Mock ISimulationTimeProvider
            var simTimeMock = new Mock<ISimulationTimeProvider>();
            services.AddSingleton(simTimeMock.Object);

            // 🧩 Remove background services that hit DB
            var hostedServiceDescriptors = services
                .Where(d => typeof(IHostedService).IsAssignableFrom(d.ServiceType))
                .ToList();
            foreach (var hosted in hostedServiceDescriptors)
                services.Remove(hosted);

            // Optionally also remove the DbContext to ensure no accidental connection
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

    
}
