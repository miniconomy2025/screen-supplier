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
        [FromServices] TargetQuantityService targetQuantityService,
        [FromServices] ILogger<TargetQuantityService> logger)
    {
        try
        {
            logger.LogInformation("Retrieving inventory status and targets");

            var status = await targetQuantityService.GetInventoryStatusAsync();

            logger.LogInformation("Retrieved inventory status: Sand {SandTotal}/{SandTarget}, Copper {CopperTotal}/{CopperTarget}, Equipment {EquipmentTotal}/{EquipmentTarget}",
                status.Sand.Total, status.Sand.Target,
                status.Copper.Total, status.Copper.Target,
                status.Equipment.Total, status.Equipment.Target);

            return Results.Ok(status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving inventory status");
            return Results.Problem("An error occurred retrieving inventory status");
        }
    }
}