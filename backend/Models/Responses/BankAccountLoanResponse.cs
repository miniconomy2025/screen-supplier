using System.Text.Json.Serialization;

namespace ScreenProducerAPI.Models.Responses
{
    public class BankAccountLoanResponse
    {
        public bool Success { get; set; }
        [JsonPropertyName("total_outstanding_amount")]
        public int TotalOutstandingAmount { get; set; }
        public List<Loan> loans { get; set; }
    }

    public class Loan
    {
        [JsonPropertyName("loan_number")]
        public string LoanNumber { get; set; }

        [JsonPropertyName("initial_amount")]
        public int InitialAmount { get; set; }
        
        [JsonPropertyName("interest_rate")]
        public int InterestRate { get; set; }
        
        [JsonPropertyName("write_off")]
        public Boolean WriteOff { get; set; }
        
        [JsonPropertyName("outstanding_amount")]
        public int OutstandingAmount { get; set; }
    }
}
