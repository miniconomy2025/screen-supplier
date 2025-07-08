namespace ScreenProducerAPI.Models.Requests;

public class LogisticsRequest
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    required public List<PickupRequestItem> Items { get; set; }
}
