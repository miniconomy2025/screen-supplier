namespace ScreenProducerAPI.Services.SupplierService.Recycler.Models;

public class RecyclerMaterial
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public float Available_Quantity_In_Kg { get; set; }
    public float Price { get; set; }
}