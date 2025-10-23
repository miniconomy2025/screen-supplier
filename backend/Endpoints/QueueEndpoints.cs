using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class QueueEndpoints
{
    public static IEndpointRouteBuilder AddQueueEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/queue", GetQueueStatusHandler)
            .Produces<QueueStatusResponse>(StatusCodes.Status200OK)
            .WithTags("Queue")
            .WithName("GetQueueStatus")
            .WithSummary("Get current queue status and metrics");

        endpoints.MapPost("/queue", ProcessQueueHandler)
            .Produces<QueueProcessResponse>(StatusCodes.Status200OK)
            .WithTags("Queue")
            .WithName("ProcessQueue")
            .WithSummary("Manually trigger queue processing");

        return endpoints;
    }

    private static IResult GetQueueStatusHandler(
    [FromServices] IPurchaseOrderQueueService queueService)
    {
        var queueCount = queueService.GetQueueCount();

        var response = new QueueStatusResponse
        {
            QueueCount = queueCount,
            Timestamp = DateTime.UtcNow
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> ProcessQueueHandler(
        [FromServices] IPurchaseOrderQueueService queueService)
    {
        var queueCountBefore = queueService.GetQueueCount();
        await queueService.ProcessQueueAsync();
        var queueCountAfter = queueService.GetQueueCount();

        var response = new QueueProcessResponse
        {
            Success = true,
            QueueCountBefore = queueCountBefore,
            QueueCountAfter = queueCountAfter,
            ProcessedAt = DateTime.UtcNow
        };

        return Results.Ok(response);
    }
}

public class QueueStatusResponse
{
    public int QueueCount { get; set; }
    public DateTime Timestamp { get; set; }
}

public class QueueProcessResponse
{
    public bool Success { get; set; }
    public int QueueCountBefore { get; set; }
    public int QueueCountAfter { get; set; }
    public DateTime ProcessedAt { get; set; }
}