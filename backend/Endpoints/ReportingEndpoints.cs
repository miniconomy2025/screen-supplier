using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder AddReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/report", GetDailyReportHandler)
            .WithTags("Reporting")
            .WithName("GetDailyReport")
            .WithSummary("Get daily report")
            .Produces<DailyReport>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public static async Task<IResult> GetMaterialsSummaryHandler(StockStatisticsService stockStatisticsService)
    {
        var materialsSummary = await stockStatisticsService.GetMaterialStatisticsAsync();

        if (materialsSummary == null)
        {
            return Results.NotFound(new { error = "No materials found" });
        }

        var materialsResponse = new MaterialsSummaryResponse
        {
            MaterialStatistics = materialsSummary
        };

        return Results.Ok(materialsResponse);
    }

    public static async Task<IResult> GetDailyReportHandler(
        [FromQuery] DateTime? date,
        [FromServices] ReportingService reportingService, [FromServices] SimulationTimeProvider simulationTimeProvider)
    {
        if (date == default)
        {
            date = simulationTimeProvider.Now.Date;
        }
        var dailyReport = await reportingService.GetDailyReportAsync(simulationTimeProvider.Now.Date);

        if (dailyReport == null)
        {
            return Results.NotFound(new { error = "No report found for the specified date." });
        }
        return Results.Ok(dailyReport);
    }
}

