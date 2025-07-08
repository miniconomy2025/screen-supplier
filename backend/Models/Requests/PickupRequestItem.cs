namespace ScreenProducerAPI.Models.Requests;
public class PickupRequestItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } // needs to be weight of machine for machine orders
    public string? MeasurementType { get; set; } = string.Empty; 
}

