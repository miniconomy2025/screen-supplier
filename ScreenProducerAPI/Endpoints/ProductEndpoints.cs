using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Mappers;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder AddProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/available-parts", GetProductsHandler)
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .WithTags("Parts")
            .WithName("GetParts");

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
