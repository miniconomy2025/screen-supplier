namespace ScreenProducerAPI.Models.Responses;

public class DropoffResponse
{
    public bool Success { get; set; }
    public int ShipmentId { get; set; }
    public int OrderId { get; set; }
    public int QuantityReceived { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}