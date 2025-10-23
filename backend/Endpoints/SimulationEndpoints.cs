using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class SimulationEndpoints
{
    public static IEndpointRouteBuilder AddSimulationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/simulation", StartSimulationHandler)
            .Accepts<SimulationStartRequest>("application/json")
            .Produces<SimulationStartResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Simulation")
            .WithName("StartSimulation")
            .WithSummary("Start the simulation with Unix epoch timestamp");

        endpoints.MapGet("/simulation", GetSimulationStatusHandler)
            .Produces<SimulationStatusResponse>(StatusCodes.Status200OK)
            .WithTags("Simulation")
            .WithName("GetSimulationStatus")
            .WithSummary("Get current simulation status and time");

        endpoints.MapDelete("/simulation", StopSimulationHandler)
            .Produces(StatusCodes.Status200OK)
            .WithTags("Simulation")
            .WithName("StopSimulation")
            .WithSummary("Stop the simulation running, and clear database");

        return endpoints;
    }

    private static async Task<IResult> StartSimulationHandler(
        SimulationStartRequest request,
        [FromServices] ISimulationTimeService simulationTimeService)
    {
        var requestTime = DateTimeOffset.FromUnixTimeMilliseconds(request.EpochStartTime);

        var success = await simulationTimeService.StartSimulationAsync(request.EpochStartTime, isResuming: false);

        var response = new SimulationStartResponse
        {
            Success = true,
            Message = "Simulation started successfully with bank integration",
            StartedAt = requestTime,
            CurrentDay = simulationTimeService.GetCurrentSimulationDay(),
            SimulationDateTime = simulationTimeService.GetSimulationDateTime()
        };

        return Results.Ok(response);
    }

    private static IResult GetSimulationStatusHandler(
        [FromServices] ISimulationTimeService simulationTimeService)
    {
        var response = new SimulationStatusResponse
        {
            IsRunning = simulationTimeService.IsSimulationRunning(),
            CurrentDay = simulationTimeService.GetCurrentSimulationDay(),
            SimulationDateTime = simulationTimeService.GetSimulationDateTime(),
            TimeUntilNextDay = simulationTimeService.GetTimeUntilNextDay()
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> StopSimulationHandler(
        [FromServices] ISimulationTimeService simulationTimeService)
    {
        await simulationTimeService.DestroySimulation();
        return Results.Ok();
    }
}