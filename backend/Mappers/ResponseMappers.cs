using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Responses;

namespace ScreenProducerAPI.Mappers;

public static class ResponseMappers
{
    public static ProductResponse MapToResponse(this Product product, int stockAvailable)
    {
        if (product == null)
        {
            return null;
        }
        return new ProductResponse()
        {
            Screens = new Screens()
            {
                Quantity = stockAvailable,
                Price = product.Price
            }
        };
    }
}
