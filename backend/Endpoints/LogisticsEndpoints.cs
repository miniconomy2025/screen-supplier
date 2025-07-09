using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class LogisticsEndpoints
{
    public static IEndpointRouteBuilder AddLogisticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/logistics", HandleLogistics)
            .Accepts<LogisticsRequest>("application/json")
            .Produces<LogisticsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("Logistics")
            .WithName("HandleLogistics")
            .WithSummary("Handle logistics operations - both deliveries and pickups");

        return endpoints;
    }

    private static async Task<IResult> HandleLogistics(
    LogisticsRequest request,
    [FromServices] LogisticsService logisticsService)
    {
        if (request?.Id <= 0 || request.Items?.FirstOrDefault()?.Quantity <= 0 || string.IsNullOrWhiteSpace(request.Type))
            throw new InvalidRequestException("Invalid logistics request. Id, Quantity must be positive and Type must be specified.");

        switch (request.Type.ToUpper())
        {
            case "DELIVERY":
                var dropoffRequest = new DropoffRequest
                {
                    Id = request.Id,
                    Quantity = request.Items[0].Quantity
                };
                var dropoffResult = await logisticsService.HandleDropoffAsync(dropoffRequest);

                return Results.Ok(new LogisticsResponse
                {
                    Success = dropoffResult.Success,
                    Id = dropoffResult.ShipmentId,
                    OrderId = dropoffResult.OrderId,
                    Quantity = dropoffResult.QuantityReceived,
                    ItemType = dropoffResult.ItemType,
                    Message = dropoffResult.Message,
                    ProcessedAt = dropoffResult.ProcessedAt
                });

            case "PICKUP":
                var collectRequest = new CollectRequest
                {
                    Id = request.Id,
                    Quantity = request.Items[0].Quantity
                };
                var collectResult = await logisticsService.HandleCollectAsync(collectRequest);

                if (collectResult == null)
                    throw new OrderNotFoundException(request.Id);

                return Results.Ok(new LogisticsResponse
                {
                    Success = collectResult.Success,
                    Id = collectResult.OrderId,
                    OrderId = collectResult.OrderId,
                    Quantity = collectResult.QuantityCollected,
                    ItemType = collectResult.ItemType,
                    Message = collectResult.Status,
                    ProcessedAt = collectResult.PreparedAt
                });

            default:
                throw new InvalidRequestException($"Unknown logistics type: {request.Type}. Must be 'DELIVERY' or 'PICKUP'.");
        }
    }
}