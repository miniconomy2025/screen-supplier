namespace ScreenProducerAPI.Models.Requests;

public class PaymentMadeRequest
{
    public Guid reference { get; set; }
    public int amount { get; set; }
}
