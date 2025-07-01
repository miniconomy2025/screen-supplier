namespace ScreenProducerAPI.Models.Responses;

public class PickupResponse
{
    public int ShipmentId { get; set; }
    public int BankAccountNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}