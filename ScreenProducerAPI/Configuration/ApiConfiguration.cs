using ScreenProducerAPI.Endpoints;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;

namespace ScreenProducerAPI.Configuration;

public static class ApiConfiguration
{
    public static void AddEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.AddProductEndpoints()
            .AddSimulationEndpoints()
            .AddLogisticsEndpoints()
            .AddPaymentEndpoints()
            .AddOrderEndpoints();

    }

    public static void AddApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddHttpClient<LogisticsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "ScreenSupplier/1.0");
        });
        services.AddOptions<BankServiceOptions>()
            .BindConfiguration($"ExternalServices:{BankServiceOptions.Section}")
            .ValidateDataAnnotations();
        services.AddHttpClient<BankService>();
        
        services.AddScoped<MaterialService>();
        services.AddScoped<ProductService>();
        services.AddScoped<EquipmentService>();
        services.AddScoped<PurchaseOrderService>();
        services.AddScoped<ScreenOrderService>();
        services.AddScoped<LogisticsService>();
        services.AddScoped<BankService>();

        services.AddSingleton<SimulationTimeService>();
    }
}
