namespace ScreenProducerAPI.Models.Requests;
public class PaymentConfirmationRequest
{
    public string ReferenceId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
}