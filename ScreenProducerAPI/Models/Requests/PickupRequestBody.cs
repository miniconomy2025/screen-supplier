namespace ScreenProducerAPI.Models.Requests;

public class PickupRequestBody
{
    public string OriginCompanyId { get; set; } = string.Empty;
    public string DestinationCompanyId { get; set; } = string.Empty;
    public string OriginalExternalOrderId { get; set; } = string.Empty;
    public List<PickupRequestItem> Items { get; set; } = new List<PickupRequestItem>();
}