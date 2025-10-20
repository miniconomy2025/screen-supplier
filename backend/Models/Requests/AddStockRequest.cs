using System.Text.Json.Serialization;

namespace ScreenProducerAPI.Models.Requests;

public class AddStockRequest
{
    [JsonPropertyName("stock_material")]
    public string? StockMaterial { get; set; }
    public int? Quantity { get; set; }
    public int? Screens { get; set; }
}
