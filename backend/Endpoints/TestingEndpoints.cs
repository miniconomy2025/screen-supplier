using Microsoft.AspNetCore.Mvc;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;

namespace ScreenProducerAPI.Endpoints;

public static class TestingEndpoints
{
    public static IEndpointRouteBuilder AddTestingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/stock", AddStockHandler)
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .WithTags("Testing")
            .WithName("AddStock");
        endpoints.MapPost("/bank-account", AddBankAccountHandler)
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .WithTags("Testing")
            .WithName("AddBankAccountTesting");

        return endpoints;
    }

    private static async Task<IResult> AddStockHandler(HttpContext context, [FromServices] IProductService productService, [FromServices] IStockStatisticsService stockStatisticsService, [FromBody] AddStockRequest addStockRequest)
    {
        try
        {
            var materialAdded = addStockRequest.StockMaterial != null;
            var screensAdded = addStockRequest.Screens != null;

            if (addStockRequest.StockMaterial != null && !string.IsNullOrEmpty(addStockRequest.StockMaterial) && (addStockRequest.Quantity != null))
            {
                var materialService = context.RequestServices.GetRequiredService<MaterialService>();
                materialAdded = await materialService.AddMaterialAsync(
                    addStockRequest.StockMaterial,
                    (int)addStockRequest.Quantity);
            }

            if (addStockRequest.Screens.HasValue && addStockRequest.Screens.Value > 0)
            {
                screensAdded = await productService.AddScreensAsync(addStockRequest.Screens.Value);
            }

            if (!materialAdded && !screensAdded)
            {
                return Results.BadRequest("Failed to add stock to database");
            }

            var quantity = await productService.GetAvailableStockAsync();
            var product = await productService.GetProductAsync();

            if (screensAdded)
            {
                var productsResponse = new ProductResponse()
                {
                    Screens = new Screens()
                    {
                        Quantity = quantity,
                        Price = product?.Price ?? 0
                    }
                };

                return Results.Ok(productsResponse);
            }

            var statistics = await stockStatisticsService.GetMaterialStatisticsAsync();

            return Results.Ok(statistics);
        }
        catch (Exception ex)
        {
            return Results.Problem("An error occurred while adding stock");
        }
    }

    private static async Task<IResult> AddBankAccountHandler(HttpContext context, [FromServices] IBankService bankService)
    {
        try
        {
            var success = await bankService.AddBankAccountAsync();

            if (bankService == null)
            {
                return Results.BadRequest("Failed to update bank account information");
            }
            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.Problem("An error occurred while updating bank account information");
        }
    }
}
