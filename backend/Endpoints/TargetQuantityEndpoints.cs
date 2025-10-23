using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class TargetQuantityEndpoints
{
    public static IEndpointRouteBuilder AddTargetQuantityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/targets", GetTargetsHandler)
            .Produces<InventoryStatus>(StatusCodes.Status200OK)
            .WithTags("Targets")
            .WithName("GetTargets")
            .WithSummary("Get current inventory status and targets");
        return endpoints;
    }

    private static async Task<IResult> GetTargetsHandler(
    [FromServices] ITargetQuantityService targetQuantityService)
    {
        var status = await targetQuantityService.GetInventoryStatusAsync();
        return Results.Ok(status);
    }
}