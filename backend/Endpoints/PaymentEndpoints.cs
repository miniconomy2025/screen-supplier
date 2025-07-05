using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder AddPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/payment", PaymentNotificationHandler)
            .Accepts<TransactionNotification>("application/json")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Payments")
            .WithName("PaymentNotification")
            .WithSummary("Process payment notification from commercial bank");

        return endpoints;
    }

    private static async Task<IResult> PaymentNotificationHandler(
        TransactionNotification notification,
        [FromServices] ScreenOrderService screenOrderService)
    {
        try
        {
            if (notification == null ||
                string.IsNullOrWhiteSpace(notification.TransactionNumber) ||
                string.IsNullOrWhiteSpace(notification.Description) ||
                notification.Amount <= 0)
            {
                return Results.BadRequest(new { error = "Invalid payment notification" });
            }

            if (!int.TryParse(notification.Description, out int orderId))
            {
                return Results.BadRequest(new { error = "Invalid order ID in description" });
            }

            var paymentRequest = new PaymentConfirmationRequest
            {
                ReferenceId = orderId.ToString(),
                AccountNumber = notification.To,
                AmountPaid = notification.Amount
            };

            var result = await screenOrderService.ProcessPaymentConfirmationAsync(paymentRequest);

            if (result?.Success == true)
            {
                return Results.Ok(new { success = true });
            }
            else
            {
                return Results.BadRequest(new { error = result?.Message ?? "Payment processing failed" });
            }
        }
        catch (Exception ex)
        {
            return Results.Problem("An unexpected error occurred processing the payment notification");
        }
    }
}