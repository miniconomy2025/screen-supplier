using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Exceptions;
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
        if (notification == null ||
            string.IsNullOrWhiteSpace(notification.Description) ||
            notification.Amount <= 0)
        {
            throw new InvalidRequestException("Invalid payment notification");
        }

        if (!int.TryParse(notification.Description, out int orderId))
        {
            throw new InvalidRequestException("Invalid order ID in description");
        }

        var result = await screenOrderService.ProcessPaymentConfirmationAsync(notification, orderId.ToString());

        if (result?.Success != true)
        {
            throw new InvalidOperationException(result?.Message ?? "Payment processing failed");
        }

        return Results.Ok(new { success = true });
    }
}