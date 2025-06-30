
using ScreenProducerAPI.Models.Requests;

namespace ScreenProducerAPI.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder AddPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/payment-made", PaymentMadeHandler)
            //.Accepts<PaymentMadeRequest>("application/json")
            //.Produces(StatusCodes.Status204NoContent)
            .WithTags("Simulation")
            .WithName("StartSimulation");

        return endpoints;
    }

    private static async Task<IResult> PaymentMadeHandler(HttpContext context)
    {
        //TODO: Handle payment
        var request = await context.Request.ReadFromJsonAsync<PaymentMadeRequest>();

        if (request == null || string.IsNullOrWhiteSpace(request.reference) || request.amount <= 0)
        {
            return Results.BadRequest("Invalid payment request.");
        }

        return Results.NoContent();
    }
}
