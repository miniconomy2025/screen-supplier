namespace ScreenProducerAPI.Models.Requests;

public class PickupRequestBody
{
    public string OriginCompany { get; set; } = string.Empty;
    public string DestinationCompany { get; set; } = string.Empty;
    public string OriginalExternalOrderId { get; set; } = string.Empty;
    public List<PickupRequestItem> Items { get; set; } = new List<PickupRequestItem>();
}