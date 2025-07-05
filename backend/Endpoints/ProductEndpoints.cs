using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Mappers;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder AddProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/screens", GetProductsHandler)
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .WithTags("Screen")
            .WithName("GetScreens")
            .WithSummary("Get screens on offer, and current price based on our average costs...");

        return endpoints;
    }

    private static async Task<IResult> GetProductsHandler(HttpContext context, [FromServices] ProductService productService)
    {
        var quantity  = await productService.GetAvailableStockAsync();

        var products = await productService.GetProductsAsync();
        var response = products.Select(product => product.MapToResponse(quantity));

        return Results.Ok(response);
    }
}
