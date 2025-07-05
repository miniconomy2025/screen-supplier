using ScreenProducerAPI.Endpoints;
using ScreenProducerAPI.Models.Configuration;
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
            .AddOrderEndpoints()
            .AddTargetQuantityEndpoints()
            .AddQueueEndpoints();
    }

    public static void AddApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // HTTP Clients
        services.AddHttpClient<LogisticsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "ScreenSupplier/1.0");
        });

        // Bank Services
        services.AddOptions<BankServiceOptions>()
            .BindConfiguration($"ExternalServices:{BankServiceOptions.Section}")
            .ValidateDataAnnotations();
        services.AddHttpClient<BankService>();
        services.AddScoped<BankIntegrationService>();

        // Bank Settings
        services.AddOptions<BankSettingsConfig>()
            .BindConfiguration("BankSettings")
            .ValidateDataAnnotations();

        // Target Quantities and Reorder Settings
        services.AddOptions<TargetQuantitiesConfig>()
            .BindConfiguration("TargetQuantities")
            .ValidateDataAnnotations();

        services.AddOptions<ReorderSettingsConfig>()
            .BindConfiguration("ReorderSettings")
            .ValidateDataAnnotations();

        // Queue Settings
        services.AddOptions<QueueSettingsConfig>()
            .BindConfiguration("QueueSettings")
            .ValidateDataAnnotations();

        // Company Info
        services.AddOptions<CompanyInfoConfig>()
            .BindConfiguration("CompanyInfo")
            .ValidateDataAnnotations();

        // Stock Management
        services.AddOptions<StockManagementOptions>()
            .BindConfiguration(StockManagementOptions.Section)
            .ValidateDataAnnotations();

        // Queue Service and Background Processing
        services.AddSingleton<PurchaseOrderQueueService>();
        services.AddHostedService<QueueProcessingBackgroundService>();

        // Core Services
        services.AddScoped<TargetQuantityService>();
        services.AddScoped<ReorderService>();


        // Business Logic Services
        services.AddScoped<MaterialService>();
        services.AddScoped<ProductService>();
        services.AddScoped<EquipmentService>();
        services.AddScoped<PurchaseOrderService>();
        services.AddScoped<ScreenOrderService>();
        services.AddScoped<LogisticsService>();
        services.AddScoped<BankService>();
        services.AddSingleton<SimulationTimeService>();
        services.AddScoped<StockStatisticsService>();

        // Time provider service
        services.AddScoped<SimulationTimeProvider>();
    }
}
