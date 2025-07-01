using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;

namespace ScreenProducerAPI.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder AddOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/order", CreateOrderHandler)
            .Accepts<CreateOrderRequest>("application/json")
            .Produces<CreateOrderResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Orders")
            .WithName("CreateOrder")
            .WithSummary("Create a new screen order");

        endpoints.MapGet("/order/{id:int}", GetOrderStatusHandler)
            .Produces<OrderStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("Orders")
            .WithName("GetOrderStatus")
            .WithSummary("Get order status and details");

        return endpoints;
    }

    private static async Task<IResult> CreateOrderHandler(
        CreateOrderRequest request,
        [FromServices] ScreenOrderService screenOrderService,
        [FromServices] BankService bankService,
        [FromServices] ILogger<CreateOrderRequest> logger)
    {
        try
        {
            // Validate request
            if (request == null || request.Quantity <= 0)
            {
                logger.LogWarning("Invalid order request: Quantity={Quantity}", request?.Quantity);
                return Results.BadRequest(new { error = "Invalid order request. Quantity must be positive." });
            }

            logger.LogInformation("Creating order for {Quantity} screens", request.Quantity);

            // Create the order
            var screenOrder = await screenOrderService.CreateOrderAsync(request.Quantity);

            if (screenOrder == null)
            {
                logger.LogWarning("Failed to create order for {Quantity} screens - likely insufficient stock", request.Quantity);
                return Results.BadRequest(new { error = "Unable to create order. Insufficient stock available." });
            }

            // Get bank account number
            var bankAccountNumber = await bankService.GetBankAccountNumberAsync();
            if (string.IsNullOrEmpty(bankAccountNumber))
            {
                logger.LogError("No bank account number configured");
                return Results.Problem("Bank account not configured");
            }

            var totalPrice = screenOrder.Quantity * screenOrder.UnitPrice;
            var response = new CreateOrderResponse
            {
                OrderId = screenOrder.Id,
                TotalPrice = totalPrice,
                BankAccountNumber = bankAccountNumber,
                OrderStatusLink = $"/order/{screenOrder.Id}"
            };

            logger.LogInformation("Created order {OrderId} for {Quantity} screens. Total: {TotalPrice}, Bank: {BankAccount}",
                screenOrder.Id, screenOrder.Quantity, totalPrice, bankAccountNumber);

            return Results.Created($"/order/{screenOrder.Id}", response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order for {Quantity} screens", request?.Quantity);
            return Results.Problem("An error occurred creating the order");
        }
    }

    private static async Task<IResult> GetOrderStatusHandler(
        int id,
        [FromServices] ScreenOrderService screenOrderService,
        [FromServices] ILogger<int> logger)
    {
        try
        {
            logger.LogInformation("Retrieving status for order {OrderId}", id);

            var screenOrder = await screenOrderService.FindScreenOrderByIdAsync(id);

            if (screenOrder == null)
            {
                logger.LogWarning("Order {OrderId} not found", id);
                return Results.NotFound(new { error = $"Order {id} not found" });
            }

            var totalPrice = screenOrder.Quantity * screenOrder.UnitPrice;
            var amountPaid = screenOrder.AmountPaid ?? 0;
            var remainingBalance = Math.Max(0, totalPrice - amountPaid);
            var isFullyPaid = amountPaid >= totalPrice;

            var response = new OrderStatusResponse
            {
                OrderId = screenOrder.Id,
                Quantity = screenOrder.Quantity,
                UnitPrice = screenOrder.UnitPrice,
                TotalPrice = totalPrice,
                Status = screenOrder.OrderStatus?.Status ?? "unknown",
                OrderDate = screenOrder.OrderDate,
                AmountPaid = screenOrder.AmountPaid,
                RemainingBalance = remainingBalance,
                IsFullyPaid = isFullyPaid
            };

            logger.LogInformation("Retrieved order {OrderId} status: {Status}, Paid: {AmountPaid}/{TotalPrice}",
                id, response.Status, amountPaid, totalPrice);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order status for order {OrderId}", id);
            return Results.Problem("An error occurred retrieving the order status");
        }
    }
}