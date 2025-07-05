using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints
{
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

            return endpoints;
        }

        private static async Task<IResult> StartSimulationHandler(
            SimulationStartRequest request,
            [FromServices] SimulationTimeService simulationTimeService)
        {
            try
            {
                var requestTime = DateTimeOffset.FromUnixTimeSeconds(request.UnixEpochStart);

                var success = await simulationTimeService.StartSimulationAsync(request.UnixEpochStart);

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
            catch (Exception ex)
            {
               return Results.Problem($"Failed to start simulation: {ex.Message}");
            }
        }

        private static IResult GetSimulationStatusHandler(
            [FromServices] SimulationTimeService simulationTimeService)
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
    }
}