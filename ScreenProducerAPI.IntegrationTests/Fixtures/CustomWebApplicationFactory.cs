using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScreenProducerAPI.IntegrationTests.Mocks;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;

namespace ScreenProducerAPI.IntegrationTests.Fixtures;


/// - Replaces the real PostgreSQL database with an in-memory database
/// - Replaces external HTTP services (Bank, Hand, Recycler, Logistics) with mocks
/// - Keeps all internal business logic services real

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "IntegrationTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL db registrations
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


            services.AddDbContext<ScreenContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            }, ServiceLifetime.Scoped);

            ReplaceExternalServices(services);
        });

        builder.UseEnvironment("Testing");
    }

    private void ReplaceExternalServices(IServiceCollection services)
    {
        services.RemoveAll<IHandService>();
        services.RemoveAll<HandService>();
        services.RemoveAll<IRecyclerService>();
        services.RemoveAll<RecyclerService>();
        services.RemoveAll<IBankService>();
        services.RemoveAll<BankService>();
        services.RemoveAll<ILogisticsService>();
        services.RemoveAll<LogisticsService>();

        var mockHandService = new MockHandService();
        services.AddScoped<IHandService>(sp => mockHandService);
        services.AddScoped<HandService>(sp => new FakeHandService(mockHandService));

        services.AddScoped<IRecyclerService, MockRecyclerService>();
        services.AddScoped<IBankService, MockBankService>();
        services.AddScoped<ILogisticsService, MockLogisticsService>();
    }

    private class FakeHandService : HandService
    {
        private readonly IHandService _mockService;

        public FakeHandService(IHandService mockService) : base(null!, null!, null!)
        {
            _mockService = mockService;
        }
    }

    public void SeedTestData()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScreenContext>();

        if (context.OrderStatuses.Any())
            return;

        context.OrderStatuses.AddRange(
            new Models.OrderStatus { Id = 1, Status = "waiting_payment" },
            new Models.OrderStatus { Id = 2, Status = "waiting_collection" },
            new Models.OrderStatus { Id = 3, Status = "collected" }
        );

        context.SaveChanges();
    }
}
