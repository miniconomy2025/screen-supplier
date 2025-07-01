namespace ScreenProducerAPI.Models.Requests;

public class MakePaymentRequest
{
    public string ToAccountNumber { get; set; }
    public int ToBankId { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; }
}
