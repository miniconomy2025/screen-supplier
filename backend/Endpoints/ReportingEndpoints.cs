using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Models;
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

        endpoints.MapGet("/report/period", GetLastPeriodReportsHandler)
            .WithTags("Reporting")
            .WithName("GetLastPeriodReport")
            .WithSummary("Get last period report")
            .Produces<DailyReport>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapGet("/report/purchases", GetPurchaseOrdersHandler)
            .WithTags("Reporting")
            .WithName("GetPurchaseOrders")
            .WithSummary("Get purchase orders")
            .Produces<IEnumerable<PurchaseOrder>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public static async Task<IResult> GetDailyReportHandler(
        [FromQuery] DateTime? date,
        [FromServices] ReportingService reportingService, [FromServices] SimulationTimeProvider simulationTimeProvider)
    {
        var dailyReport = await reportingService.GetDailyReportAsync(date ?? simulationTimeProvider.Now.Date);

        if (dailyReport == null)
        {
            return Results.NotFound(new { error = "No report found for the specified date." });
        }
        return Results.Ok(dailyReport);
    }

    public static async Task<IResult> GetLastPeriodReportsHandler(
        [FromQuery] int pastDaysToInclude,
        [FromServices] ReportingService reportingService, [FromServices] SimulationTimeProvider simulationTimeProvider)
    {
        if (pastDaysToInclude <= 0 || pastDaysToInclude > 90)
        {
            return Results.BadRequest(new { error = "Invalid number of days specified. Please provide a value between 1 and 90." });
        }

        var reports = await reportingService.GetLastPeriodReportsAsync(pastDaysToInclude, simulationTimeProvider.Now.Date);

        if (reports == null)
        {
            return Results.NotFound(new { error = "No reports found for the last seven days." });
        }
        return Results.Ok(reports);
    }

    public static async Task<IResult> GetPurchaseOrdersHandler(
        [FromQuery] DateTime? date,
        [FromServices] PurchaseOrderService purchaseOrderService, [FromServices] SimulationTimeProvider simulationTimeProvider)
    {
        var purchaseOrders = await purchaseOrderService.GetPastOrdersAsync(date ?? simulationTimeProvider.Now.Date);
        if (purchaseOrders == null || purchaseOrders.Count == 0)
        {
            return Results.NotFound(new { error = "No purchase orders found for the specified date." });
        }
        return Results.Ok(purchaseOrders);
    }
}

