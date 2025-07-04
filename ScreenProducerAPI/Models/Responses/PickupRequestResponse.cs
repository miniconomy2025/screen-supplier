namespace ScreenProducerAPI.Models.Responses;

public class PickupRequestResponse
{
    public bool? Success { get; set; }
    public string PickupRequestId { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string? Message { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}