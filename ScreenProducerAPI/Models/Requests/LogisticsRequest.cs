namespace ScreenProducerAPI.Models.Requests;

public class LogisticsRequest
{
    public int Id { get; set; } // orderId for pickup, shipmentId for delivery
    public string Type { get; set; } = string.Empty; // "PICKUP" or "DELIVERY"
    public int Quantity { get; set; }
}
