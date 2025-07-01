namespace ScreenProducerAPI.Models.Responses;

public class RepayLoanResponse
{
    public bool Success { get; set; }
    public int Paid { get; set; }
    public int Overpayment { get; set; }
}
