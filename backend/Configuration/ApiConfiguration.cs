﻿using ScreenProducerAPI.Commands.Queue;
using ScreenProducerAPI.Endpoints;
using ScreenProducerAPI.Middleware;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Services.SupplierService;
using System.Net.Security;
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
        app.UseMiddleware<ExceptionHandlingMiddleware>();


        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ScreenProducerAPI v1"));


        app.UseHttpsRedirection();

        app.AddEndpoints();
        app.UseRateLimiter();

    }

    public static void AddApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        X509Certificate2 clientCertificate = CreatePfx();

        // HTTP Clients
        services.AddHttpClient<LogisticsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "ScreenSupplier/1.0");
        }).ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
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
                ServerCertificateCustomValidationCallback = ValidateServerCertificate,
                ClientCertificates = { clientCertificate }
            });
        services.AddHttpClient<HandService>(client =>
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = ValidateServerCertificate,
                ClientCertificates = { clientCertificate }
            };

            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "ScreenSupplier/1.0");
        }).ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
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
        services.AddSingleton<SimulationTimeService>();
        services.AddScoped<StockStatisticsService>();
        services.AddScoped<ProductionHistoryService>();
        services.AddScoped<ReportingService>();

        services.AddScoped<IQueueCommandFactory, QueueCommandFactory>();

        // Time provider service
        services.AddScoped<SimulationTimeProvider>();
    }

    private static bool ValidateServerCertificate(HttpRequestMessage message, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        return true;
    }

    private static X509Certificate2 CreatePfx()
    {

        string certPem = File.ReadAllText(@"../screen-supplier-client.crt");
        string keyPem = File.ReadAllText(@"../screen-supplier-client.key");
        X509Certificate2 cert = X509Certificate2.CreateFromPem(certPem, keyPem);

        var pfxCertificate = new X509Certificate2(cert.Export(X509ContentType.Pfx));

        return pfxCertificate;

    }
}
