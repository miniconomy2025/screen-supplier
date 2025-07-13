namespace ScreenProducerAPI.Services.SupplierService.Recycler.Models;

public class RecyclerMaterial
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public float AvailableQuantityInKg { get; set; }
    public float PricePerKg { get; set; }
}