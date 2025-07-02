namespace ScreenProducerAPI.Models.Responses;

public class PickupRequestResponse
{
    public bool Success { get; set; }
    public string PickupRequestId { get; set; } = string.Empty; // This becomes our shipmentId
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}