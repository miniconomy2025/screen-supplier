namespace ScreenProducerAPI.Models.Responses;

public class PickupRequestResponse
{
    public bool? Success { get; set; }
    public int PickupRequestId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Cost { get; set; }
    public string? Message { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}