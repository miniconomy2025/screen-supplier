namespace ScreenProducerAPI.Services.SupplierService.Hand.Models;

public class MachineForSale
{
    public string MachineName { get; set; }
    public int Quantity { get; set; }
    public int Price { get; set; }
    public inputRatio InputRatio { get; set; }
    public int ProductionRate { get; set; }
    public int Weight { get; set; }
}


public class inputRatio
{
    public int Copper { get; set; }
    public int Sand { get; set; }
}