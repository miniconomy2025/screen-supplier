namespace ScreenProducerAPI.Services.SupplierService.Recycler.Models;

public class RecyclerOrderDetailItem
{
    public string Material { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public float Price { get; set; }
}