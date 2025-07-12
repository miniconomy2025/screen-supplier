using System.Text.Json.Serialization;

namespace ScreenProducerAPI.Models.Requests;
public class TransactionNotification
{
    [JsonPropertyName("transaction_number")]
    public string? TransactionNumber { get; set; } = string.Empty;
    public string? Status { get; set; } = string.Empty;
    public int Amount { get; set; }
    public double? Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? To { get; set; } = string.Empty;
    public string? From { get; set; } = string.Empty;
}
