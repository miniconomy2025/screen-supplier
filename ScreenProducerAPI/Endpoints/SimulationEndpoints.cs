namespace ScreenProducerAPI.Endpoints;

public static class SimulationEndpoints
{
    public static IEndpointRouteBuilder AddSimulationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/simulation/start", StartSimulationHandler)
            //.Produces(StatusCodes.Status204NoContent)
            .WithTags("Simulation")
            .WithName("StartSimulation");

        return endpoints;
    }

    private static async Task<IResult> StartSimulationHandler(HttpContext context)
    {
        //TODO: Start internal simulation
        return Results.NoContent();
    }
}
