using System.Text.Json.Serialization;

namespace ScreenProducerAPI.Models.Responses
{
    public class BankAccountLoanResponse
    {
        public bool Success { get; set; }
        [JsonPropertyName("total_outstanding_amount")]
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string TotalOutstandingAmount { get; set; }
        public List<Loan> loans { get; set; }
    }

    public class Loan
    {
        [JsonPropertyName("loan_number")]
        public string LoanNumber { get; set; }

        [JsonPropertyName("initial_amount")]
        public string InitialAmount { get; set; }
        
        [JsonPropertyName("interest_rate")]
        public string InterestRate { get; set; }
        
        [JsonPropertyName("write_off")]
        public Boolean WriteOff { get; set; }
        
        [JsonPropertyName("outstanding_amount")]
        public string OutstandingAmount { get; set; }
    }
}
