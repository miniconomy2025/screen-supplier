namespace ScreenProducerAPI.Services.SupplierService.Recycler.Models;

public class RecyclerOrderRequest
{
    public string CompanyName { get; set; }
    public List<RecyclerOrderItem> OrderItems { get; set; } = [];
}