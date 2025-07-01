namespace ScreenProducerAPI.Models.Requests;

public class MakePaymentRequest
{
    public string Reference { get; set; }
    public int Amount { get; set; }
}
