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
            [FromServices] SimulationTimeService simulationTimeService,
            [FromServices] EquipmentService equipmentService,
            [FromServices] ILogger<SimulationStartRequest> logger)
        {
            try
            {
                var requestTime = DateTimeOffset.FromUnixTimeSeconds(request.UnixEpochStart);

                logger.LogInformation("Starting simulation with Unix epoch {Epoch} ({DateTime})",
                    request.UnixEpochStart, requestTime.ToString("yyyy-MM-dd HH:mm:ss UTC"));

                // Initialize equipment parameters if not already done
                // 1kg sand + 1kg copper = 500 screens per day
                // this will be done in a call to the hand for state (might need to move it)
                await equipmentService.InitializeEquipmentParametersAsync(1, 1, 500);

                // Start the simulation
                simulationTimeService.StartSimulation(request.UnixEpochStart);

                var response = new SimulationStartResponse
                {
                    Success = true,
                    Message = "Simulation started successfully",
                    StartedAt = requestTime,
                    CurrentDay = simulationTimeService.GetCurrentSimulationDay(),
                    SimulationDateTime = simulationTimeService.GetSimulationDateTime()
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start simulation");
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