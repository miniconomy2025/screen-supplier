namespace ScreenProducerAPI.Models.Requests;

public class PaymentMadeRequest
{
    public string reference { get; set; }
    public int amount { get; set; }
}
