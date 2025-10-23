using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.SupplierService.Recycler.Models;

namespace ScreenProducerAPI.IntegrationTests.Mocks;

public class MockRecyclerService : IRecyclerService
{
    private readonly List<RecyclerOrderDetailResponse> _orders = new();
    private static int _orderIdCounter = 1;

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
            },
            new RecyclerMaterial
            {
                Id = 3,
                Name = "Steel",
                AvailableQuantityInKg = 3000,
                PricePerKg = 25
            }
        };

        return Task.FromResult(materials);
    }

    public Task<RecyclerOrderCreatedResponse> CreateOrderAsync(RecyclerOrderRequest request)
    {
        // Simulate error conditions for specific test scenarios
        if (request.CompanyName == "INSUFFICIENT_STOCK_COMPANY")
        {
            throw new InsufficientStockException("recycled materials", 1, 0);
        }

        if (request.CompanyName == "NETWORK_ERROR_COMPANY")
        {
            throw new RecyclerServiceException("Recycler service unavailable for order creation",
                new HttpRequestException("Network error"));
        }

        if (request.CompanyName == "TIMEOUT_COMPANY")
        {
            throw new RecyclerServiceException("Recycler service timeout during order creation",
                new TaskCanceledException("Timeout"));
        }

        var orderNumber = "RECYC-" + Guid.NewGuid().ToString()[..8];

        var orderDetail = new RecyclerOrderDetailResponse
        {
            OrderNumber = orderNumber,
            SupplierName = "Mock Recycler",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            Items = request.OrderItems?.Select(item => new RecyclerOrderDetailItem
            {
                Material = item.RawMaterialName,
                Quantity = item.QuantityInKg,
                Price = item.QuantityInKg * (item.RawMaterialName.ToLower() == "sand" ? 8f : 40f)
            }).ToList() ?? new List<RecyclerOrderDetailItem>()
        };

        _orders.Add(orderDetail);

        var response = new RecyclerOrderCreatedResponse
        {
            data = new Data
            {
                OrderId = System.Threading.Interlocked.Increment(ref _orderIdCounter),
                AccountNumber = "MOCK-RECYCLER-ACC",
                OrderItems = request.OrderItems?.Select(item => new OrderItem
                {
                    QuantityInKg = item.QuantityInKg,
                    PricePerKg = item.RawMaterialName.ToLower() == "sand" ? 8m : 40m
                }).ToList() ?? new List<OrderItem>()
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
        // Simulate error conditions for specific test scenarios
        if (orderNumber == "NETWORK_ERROR_ORDER")
        {
            throw new RecyclerServiceException($"Recycler service unavailable for order {orderNumber} retrieval",
                new HttpRequestException("Network error"));
        }

        if (orderNumber == "TIMEOUT_ORDER")
        {
            throw new RecyclerServiceException($"Recycler service timeout during order {orderNumber} retrieval",
                new TaskCanceledException("Timeout"));
        }

        if (orderNumber == "NOT_FOUND_ORDER")
        {
            throw new DataNotFoundException($"Recycler order {orderNumber}");
        }

        var order = _orders.FirstOrDefault(o => o.OrderNumber == orderNumber);

        if (order == null)
        {
            throw new DataNotFoundException($"Recycler order {orderNumber}");
        }

        return Task.FromResult(order);
    }
}
