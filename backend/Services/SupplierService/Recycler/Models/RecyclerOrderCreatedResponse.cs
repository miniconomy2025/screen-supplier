namespace ScreenProducerAPI.Services.SupplierService.Recycler.Models;

public class RecyclerOrderCreatedResponse
{
    public Data data { get; set; }
}


public class Data
{
    public int OrderId { get; set; }
    public string AccountNumber { get; set; }
    public List<ItemsInPurchase> OrderItems { get; set; }
}


public class ItemsInPurchase
{
    public int quantityInKg { get; set; }
    public int pricePerKg { get; set; }
}
