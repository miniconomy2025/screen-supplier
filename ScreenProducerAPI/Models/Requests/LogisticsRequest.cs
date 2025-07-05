namespace ScreenProducerAPI.Models.Requests;

public class LogisticsRequest
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
