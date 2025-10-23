using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class MachineFailureEndpoints
{
    public static IEndpointRouteBuilder AddMachineFailureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/machine-failure", HandleMachineFailure)
            .Accepts<MachineFailureRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("Machines")
            .WithName("HandleMachineFailure")
            .WithSummary("Handles machine failure events from the Hand of Hḗphaistos by marking equipment as unavailable.");

        return endpoints;
    }

    private static async Task<IResult> HandleMachineFailure(
        MachineFailureRequest request,
        [FromServices] IEquipmentService equipmentService)
    {
        try
        {
            
            if (request.FailureQuantity <= 0)
            {
                return Results.BadRequest(new
                {
                    error = "Failure quantity must be greater than 0."
                });
            }

            var result = await equipmentService.ProcessMachineFailureAsync(request.FailureQuantity );

            if (!result.Success)
            {
                return Results.NotFound(new
                {
                    error = result.Message
                });
            }

            return Results.Ok(new
            {
                message = result.Message,
                failedCount = result.FailedCount,
                simulationDate = request.SimulationDate.ToString("yyyy-MM-dd"),
                simulationTime = request.SimulationTime.ToString()
            });
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred while processing the machine failure event.");
        }
    }
}
