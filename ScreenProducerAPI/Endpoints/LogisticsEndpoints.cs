using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class LogisticsEndpoints
{
    public static IEndpointRouteBuilder AddLogisticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/dropoff", HandleDropoff)
            .Accepts<DropoffRequest>("application/json")
            .Produces<DropoffResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Logistics")
            .WithName("HandleDropoff")
            .WithSummary("Handle materials/equipment delivery from logistics company");

        endpoints.MapPost("/collect", HandleCollect)
            .Accepts<CollectRequest>("application/json")
            .Produces<CollectResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("Logistics")
            .WithName("HandleCollect")
            .WithSummary("Handle screen collection by logistics company");

        return endpoints;
    }

    private static async Task<IResult> HandleDropoff(
        DropoffRequest request,
        [FromServices] LogisticsService logisticsService)
    {
        try
        {
            if (request == null || request.Id <= 0 || request.Quantity <= 0)
            {
                return Results.BadRequest(new { error = "Invalid dropoff request. Id (shipmentId) and Quantity must be positive." });
            }

            var result = await logisticsService.HandleDropoffAsync(request);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred processing the dropoff");
        }
    }

    private static async Task<IResult> HandleCollect(
        CollectRequest request,
        [FromServices] LogisticsService logisticsService)
    {
        try
        {
            if (request == null || request.Id <= 0 || request.Quantity <= 0)
            {
                return Results.BadRequest(new { error = "Invalid collect request. Id (orderId) and Quantity must be positive." });
            }

            var result = await logisticsService.HandleCollectAsync(request);
            if (result == null)
            {
                return Results.NotFound(new { error = "Order not found or not ready for collection" });
            }
            
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred preparing the collection");
        }
    }
}