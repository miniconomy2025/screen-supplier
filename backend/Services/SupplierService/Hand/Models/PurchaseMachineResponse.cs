namespace ScreenProducerAPI.Services.SupplierService.Hand.Models;

public class PurchaseMachineResponse
{
    public int OrderId { get; set; }
    public string MachineName { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal UnitWeight { get; set; }
    public MachineDetails? MachineDetails { get; set; }
    public string BankAccount { get; set; }
}