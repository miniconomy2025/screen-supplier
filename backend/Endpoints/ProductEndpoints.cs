using Microsoft.AspNetCore.Mvc;
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

        var product = await productService.GetProductAsync();
        var productsResponse = new ProductResponse()
        {
            Screens = new Screens()
            {
                Quantity = quantity,
                Price = product.Price
            }
        };

        return Results.Ok(productsResponse);
    }
}
