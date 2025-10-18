using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Services.SupplierService;
using ScreenProducerAPI.IntegrationTests.Mocks;

namespace ScreenProducerAPI.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// This factory:
/// - Replaces the real PostgreSQL database with an in-memory database
/// - Replaces external HTTP services (Bank, Hand, Recycler, Logistics) with mocks
/// - Keeps all internal business logic services real
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "IntegrationTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL database-related registrations (PostgreSQL, DbContext, DbContextPool, DbContextOptions)
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(ScreenContext) ||
                           d.ServiceType == typeof(DbContextOptions<ScreenContext>) ||
                           d.ServiceType.ToString().Contains("DbContextPool") ||
                           d.ServiceType.ToString().Contains("DbContextOptions"))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for integration tests (replaces PostgreSQL)
            // Use a stable database name for this factory instance
            services.AddDbContext<ScreenContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            }, ServiceLifetime.Scoped);

            // Replace external HTTP service dependencies with mocks
            ReplaceExternalServices(services);
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Replace external HTTP services with mock implementations
    /// </summary>
    private void ReplaceExternalServices(IServiceCollection services)
    {
        // Remove real external service implementations
        services.RemoveAll<IHandService>();
        services.RemoveAll<HandService>();
        services.RemoveAll<IRecyclerService>();
        services.RemoveAll<RecyclerService>();
        services.RemoveAll<IBankService>();
        services.RemoveAll<BankService>();
        // Note: LogisticsService doesn't have an interface, so we keep it as-is

        // Add mock implementations
        var mockHandService = new MockHandService();
        services.AddScoped<IHandService>(sp => mockHandService);
        services.AddScoped<HandService>(sp => new FakeHandService(mockHandService));

        services.AddScoped<IRecyclerService, MockRecyclerService>();
        services.AddScoped<IBankService, MockBankService>();
    }

    /// <summary>
    /// Fake HandService that delegates to MockHandService (to satisfy Program.cs requiring HandService)
    /// </summary>
    private class FakeHandService : HandService
    {
        private readonly IHandService _mockService;

        public FakeHandService(IHandService mockService) : base(null!, null!, null!)
        {
            _mockService = mockService;
        }
    }

    /// <summary>
    /// Seed test data into the in-memory database
    /// Call this from tests that need pre-seeded data
    /// </summary>
    public void SeedTestData()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

        // Only seed if database is empty
        if (context.OrderStatuses.Any())
            return;

        // Seed order statuses (required for most tests)
        context.OrderStatuses.AddRange(
            new Models.OrderStatus { Id = 1, Status = "waiting_payment" },
            new Models.OrderStatus { Id = 2, Status = "waiting_collection" },
            new Models.OrderStatus { Id = 3, Status = "collected" }
        );

        context.SaveChanges();
    }
}
