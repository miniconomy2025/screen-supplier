using System.Text.Json.Serialization;

namespace ScreenProducerAPI.Models.Responses;

public class BankAccountResponse
{
    [JsonPropertyName("account_number")]
    public string AccountNumber { get; set; }
}
