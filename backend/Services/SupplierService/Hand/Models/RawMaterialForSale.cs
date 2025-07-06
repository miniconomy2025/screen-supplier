namespace ScreenProducerAPI.Services.SupplierService.Hand.Models;

public class RawMaterialForSale
{
    public string RawMaterialName { get; set; }
    public decimal PricePerKg { get; set; }
    public int QuantityAvailable { get; set; }
}