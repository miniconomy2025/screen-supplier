namespace ScreenProducerAPI.Services.SupplierService.Hand.Models;

public class PurchaseRawMaterialResponse
{
    public int OrderId { get; set; }
    public string MaterialName { get; set; }
    public decimal WeightQuantity { get; set; }
    public decimal Price { get; set; }
    public string BankAccount { get; set; }
}