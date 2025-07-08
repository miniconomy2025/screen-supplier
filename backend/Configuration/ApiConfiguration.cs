using ScreenProducerAPI.Endpoints;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Services.SupplierService;
using ScreenProducerAPI.Services.SupplierService.Hand;
using ScreenProducerAPI.Services.SupplierService.Recycler;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

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
            .AddQueueEndpoints()
            .AddReportingEndpoints();
    }

    public static void ConfigureApp(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ScreenProducerAPI v1"));
        }

        app.Use(async (context, next) =>
        {
            //Does grab the request, difficult to test without a real service

            X509Certificate2 clientCertificate = context.Connection.ClientCertificate;

            if (clientCertificate == null || !clientCertificate.Verify())
            {
                context.Response.StatusCode = 401;
                await context.Response.CompleteAsync();
                return;
            }

            //Cert validation

            await next.Invoke();
        });

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();

        app.AddEndpoints();
        app.UseRateLimiter();

    }

    public static void AddApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        X509Certificate2 clientCertificate = new X509Certificate2("test-file-dont-panic.pfx", "YourPassword");

        // HTTP Clients
        services.AddHttpClient<LogisticsService>(client =>
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = ValidateServerCertificate,
                ClientCertificates = { clientCertificate }
            };

            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "ScreenSupplier/1.0");
        }).ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = ValidateServerCertificate,
                ClientCertificates = { clientCertificate }
            });

        services.AddHttpClient<RecyclerService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "ScreenSupplier/1.0");
        }).ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = ValidateServerCertificate,
                ClientCertificates = { clientCertificate }
            });
        services.AddHttpClient<HandService>(client =>
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = ValidateServerCertificate,
                ClientCertificates = { clientCertificate }
            };

            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "ScreenSupplier/1.0");
        }).ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = ValidateServerCertificate,
                ClientCertificates = { clientCertificate }
            });

        services.AddHttpClient<BankService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "ScreenSupplier/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = ValidateServerCertificate,
                ClientCertificates = { clientCertificate }
            });

        // Bank Services
        services.AddOptions<BankServiceOptions>()
            .BindConfiguration($"ExternalServices:{BankServiceOptions.Section}")
            .ValidateDataAnnotations();

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

        //Supplier options
        services.AddOptions<SupplierServiceOptions>()
            .BindConfiguration($"ExternalServices:{SupplierServiceOptions.Section}")
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
        services.AddSingleton<SimulationTimeService>();
        services.AddScoped<StockStatisticsService>();
        services.AddScoped<ProductionHistoryService>();
        services.AddScoped<ReportingService>();

        // Time provider service
        services.AddScoped<SimulationTimeProvider>();
    }

    private static bool ValidateServerCertificate(HttpRequestMessage message, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        return true;
    }
}
