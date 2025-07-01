using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder AddPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/payment-confirmation", PaymentConfirmationHandler)
            .Accepts<PaymentConfirmationRequest>("application/json")
            .Produces<PaymentConfirmationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Payments")
            .WithName("PaymentConfirmation")
            .WithSummary("Process payment confirmation from bank for screen orders");

        return endpoints;
    }

    private static async Task<IResult> PaymentConfirmationHandler(
        PaymentConfirmationRequest request,
        [FromServices] ScreenOrderService screenOrderService,
        [FromServices] ILogger<PaymentConfirmationRequest> logger)
    {
        try
        {
            // Basic validation
            if (request == null ||
                string.IsNullOrWhiteSpace(request.ReferenceId) ||
                string.IsNullOrWhiteSpace(request.AccountNumber) ||
                request.AmountPaid <= 0)
            {
                logger.LogWarning("Invalid payment confirmation request: ReferenceId={ReferenceId}, AccountNumber={AccountNumber}, AmountPaid={AmountPaid}",
                    request?.ReferenceId, request?.AccountNumber, request?.AmountPaid);

                return Results.BadRequest(new PaymentConfirmationResponse
                {
                    Success = false,
                    Message = "Invalid payment request. ReferenceId, AccountNumber, and AmountPaid are required and AmountPaid must be positive.",
                    ProcessedAt = DateTime.UtcNow
                });
            }

            logger.LogInformation("Received payment confirmation: ReferenceId={ReferenceId}, AccountNumber={AccountNumber}, AmountPaid={AmountPaid}",
                request.ReferenceId, request.AccountNumber, request.AmountPaid);

            // Process the payment
            var result = await screenOrderService.ProcessPaymentConfirmationAsync(request);

            if (result == null)
            {
                logger.LogError("Payment processing returned null result for ReferenceId={ReferenceId}", request.ReferenceId);
                return Results.Problem("An error occurred processing the payment confirmation");
            }

            if (result.Success)
            {
                logger.LogInformation("Payment confirmation processed successfully for order {OrderId}: {Message}",
                    result.OrderId, result.Message);
                return Results.Ok(result);
            }
            else
            {
                logger.LogWarning("Payment confirmation failed for ReferenceId={ReferenceId}: {Message}",
                    request.ReferenceId, result.Message);
                return Results.BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing payment confirmation for ReferenceId={ReferenceId}",
                request?.ReferenceId);

            return Results.Problem("An unexpected error occurred processing the payment confirmation");
        }
    }
}