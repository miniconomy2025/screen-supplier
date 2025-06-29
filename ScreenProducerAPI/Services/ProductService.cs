using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;

namespace ScreenProducerAPI.Services;

public class ProductService(ScreenContext context)
{
    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        try
        {
            return await context.Products.ToListAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while retrieving products.", ex);
        }
    }
}
