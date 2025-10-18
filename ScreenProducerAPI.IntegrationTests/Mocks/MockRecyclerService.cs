using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.SupplierService.Recycler.Models;

namespace ScreenProducerAPI.IntegrationTests.Mocks;

/// <summary>
/// Mock implementation of IRecyclerService for integration testing.
/// Returns predictable test data instead of making real HTTP calls.
/// </summary>
public class MockRecyclerService : IRecyclerService
{
    private readonly List<RecyclerOrderDetailResponse> _orders = new();

    public Task<List<RecyclerMaterial>> GetMaterialsAsync()
    {
        var materials = new List<RecyclerMaterial>
        {
            new RecyclerMaterial
            {
                Id = 1,
                Name = "Sand",
                AvailableQuantityInKg = 10000,
                PricePerKg = 8
            },
            new RecyclerMaterial
            {
                Id = 2,
                Name = "Copper",
                AvailableQuantityInKg = 5000,
                PricePerKg = 40
            }
        };

        return Task.FromResult(materials);
    }

    public Task<RecyclerOrderCreatedResponse> CreateOrderAsync(RecyclerOrderRequest request)
    {
        var orderNumber = "RECYC-" + Guid.NewGuid().ToString()[..8];

        var orderDetail = new RecyclerOrderDetailResponse
        {
            OrderNumber = orderNumber,
            SupplierName = "Mock Recycler",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            Items = new List<RecyclerOrderDetailItem>()
        };

        _orders.Add(orderDetail);

        var response = new RecyclerOrderCreatedResponse
        {
            data = new Data
            {
                OrderId = _orders.Count,
                AccountNumber = "MOCK-RECYCLER-ACC",
                OrderItems = new List<OrderItem>()
            }
        };

        return Task.FromResult(response);
    }

    public Task<List<RecyclerOrderSummaryResponse>> GetOrdersAsync()
    {
        var summaries = _orders.Select(o => new RecyclerOrderSummaryResponse
        {
            OrderNumber = o.OrderNumber,
            SupplierName = o.SupplierName,
            Status = o.Status,
            CreatedAt = o.CreatedAt
        }).ToList();

        return Task.FromResult(summaries);
    }

    public Task<RecyclerOrderDetailResponse> GetOrderByNumberAsync(string orderNumber)
    {
        var order = _orders.FirstOrDefault(o => o.OrderNumber == orderNumber);

        if (order == null)
        {
            order = new RecyclerOrderDetailResponse
            {
                OrderNumber = orderNumber,
                SupplierName = "Unknown",
                Status = "NotFound",
                CreatedAt = DateTime.UtcNow,
                Items = new List<RecyclerOrderDetailItem>()
            };
        }

        return Task.FromResult(order);
    }
}
