using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Exceptions;
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

        endpoints.MapGet("/order/period", GetLastPeriodOrdersHandler)
            .Produces<IEnumerable<OrderStatusResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("Orders")
            .WithName("GetOrdersInPeriod")
            .WithSummary("Get orders in last period");

        return endpoints;
    }

    private static async Task<IResult> CreateOrderHandler(
        CreateOrderRequest request,
        [FromServices] IScreenOrderService screenOrderService,
        [FromServices] IBankService bankService)
    {
        if (request?.Quantity <= 0)
            throw new InvalidRequestException("Invalid order request. Quantity must be positive.");

        var screenOrder = await screenOrderService.CreateOrderAsync(request.Quantity);

        var bankAccountNumber = await bankService.GetBankAccountNumberAsync();
        if (string.IsNullOrEmpty(bankAccountNumber))
            throw new SystemConfigurationException("Bank account not configured");

        var totalPrice = screenOrder.Quantity * screenOrder.UnitPrice;
        var response = new CreateOrderResponse
        {
            OrderId = screenOrder.Id,
            TotalPrice = totalPrice,
            BankAccountNumber = bankAccountNumber,
            OrderStatusLink = $"/order/{screenOrder.Id}"
        };

        return Results.Created($"/order/{screenOrder.Id}", response);
    }


    private static async Task<IResult> GetOrderStatusHandler(
         int id,
         [FromServices] IScreenOrderService screenOrderService)
    {
        var screenOrder = await screenOrderService.FindScreenOrderByIdAsync(id);

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

        return Results.Ok(response);
    }

    public static async Task<IResult> GetLastPeriodOrdersHandler(
        [FromQuery] int pastDaysToInclude,
        [FromServices] IScreenOrderService screenOrderService, [FromServices] SimulationTimeProvider simulationTimeProvider)
    {
        if (pastDaysToInclude <= 0 || pastDaysToInclude > 90)
        {
            return Results.BadRequest(new { error = "Invalid number of days specified. Please provide a value between 1 and 90." });
        }

        var reports = await screenOrderService.GetPastOrdersAsync(
            simulationTimeProvider.Now.Date.AddDays(-pastDaysToInclude));

        if (reports == null)
        {
            return Results.NotFound(new { error = "No orders found for the last seven days." });
        }
        return Results.Ok(reports);
    }
}