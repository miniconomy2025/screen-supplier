using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Responses;

namespace ScreenProducerAPI.Mappers;

public static class ResponseMappers
{
    public static ProductResponse MapToResponse(this Product product)
    {
        if (product == null)
        {
            return null;
        }
        return new ProductResponse
        {
            Quantity = product.Quantity,
            Price = product.Price
        };
    }
}
