namespace ScreenProducerAPI.Models.Responses;

public class LoanInformationResponse
{
    public int TotalDue { get; set; }
    public IEnumerable<Loan> Loans { get; set; }
}
