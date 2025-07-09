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
                var logisticsResultDelivery = await logisticsService.HandleDropoffAsync(request.Items[0].Quantity, request.Id);

                return Results.Ok(logisticsResultDelivery);

            case "PICKUP":
                var logisticsResultCollection = await logisticsService.HandleCollectAsync(request.Items[0].Quantity, request.Id);

                if (logisticsResultCollection == null)
                    throw new OrderNotFoundException(request.Id);

                return Results.Ok(logisticsResultCollection);

            default:
                throw new InvalidRequestException($"Unknown logistics type: {request.Type}. Must be 'DELIVERY' or 'PICKUP'.");
        }
    }
}