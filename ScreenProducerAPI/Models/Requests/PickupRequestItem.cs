namespace ScreenProducerAPI.Models.Requests;
public class PickupRequestItem
{
    public string Name { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string MeasurementType { get; set; } = string.Empty; 
}

