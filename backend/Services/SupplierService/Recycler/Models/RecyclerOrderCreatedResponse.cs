namespace ScreenProducerAPI.Services.SupplierService.Recycler.Models;

public class RecyclerOrderCreatedResponse
{
    public Data data { get; set; }
}


public class Data
{
    public int OrderId { get; set; }
    public string AccountNumber { get; set; }
    public List<OrderItem> OrderItems { get; set; }
}


public class OrderItem
{
    public int QuantityInKg { get; set; }
    public decimal PricePerKg { get; set; }
}
