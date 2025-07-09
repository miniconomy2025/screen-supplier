using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ScreenProducerAPI.Configuration;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices();

builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.AddFixedWindowLimiter("RegisterEndpointLimiter", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromSeconds(30);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://screen-supplier.projects.bbdgrad.com")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});


builder.Services.AddDbContextPool<ScreenContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")), poolSize: 128);

var app = builder.Build();

app.UseCors("AllowFrontend");

app.ConfigureApp();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    var simulationService = app.Services.GetRequiredService<SimulationTimeService>();
    if (simulationService.IsSimulationRunning())
    {
        simulationService.StopSimulation();
    }
});

app.Run();

