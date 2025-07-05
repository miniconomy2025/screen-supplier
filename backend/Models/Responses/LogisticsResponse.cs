namespace ScreenProducerAPI.Models.Responses;

public class LogisticsResponse
{
    public bool Success { get; set; }
    public int Id { get; set; } // shipmentId for delivery, orderId for pickup
    public int OrderId { get; set; }
    public int Quantity { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}