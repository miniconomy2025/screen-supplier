using System.Text.Json.Serialization;

namespace ScreenProducerAPI.Models.Responses;

public class BankAccountBalanceResponse
{
    [JsonPropertyName("balance")]
    public int Balance { get; set; }
    [JsonPropertyName("account_number")]
    public string AccountNumber { get; set; }
}
