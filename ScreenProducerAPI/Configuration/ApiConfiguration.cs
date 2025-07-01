using ScreenProducerAPI.Endpoints;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Configuration;

public static class ApiConfiguration
{
    public static void AddEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.AddProductEndpoints()
            .AddSimulationEndpoints()
            .AddLogisticsEndpoints();

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

        services.AddScoped<MaterialService>();
        services.AddScoped<ProductService>();
        services.AddScoped<EquipmentService>();
        services.AddScoped<PurchaseOrderService>();
        services.AddScoped<ScreenOrderService>();
        services.AddScoped<LogisticsService>();
    }
}
