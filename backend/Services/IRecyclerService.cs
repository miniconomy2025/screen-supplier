using ScreenProducerAPI.Services.SupplierService.Recycler.Models;

namespace ScreenProducerAPI.Services;

public interface IRecyclerService
{
    Task<List<RecyclerMaterial>> GetMaterialsAsync();
    Task<RecyclerOrderCreatedResponse> CreateOrderAsync(RecyclerOrderRequest request);
    Task<List<RecyclerOrderSummaryResponse>> GetOrdersAsync();
    Task<RecyclerOrderDetailResponse> GetOrderByNumberAsync(string orderNumber);
}