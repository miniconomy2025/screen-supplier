namespace ScreenProducerAPI.Models.Responses;

public class CollectResponse
{
    public bool Success { get; set; }
    public int OrderId { get; set; }
    public int QuantityCollected { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PreparedAt { get; set; }
}