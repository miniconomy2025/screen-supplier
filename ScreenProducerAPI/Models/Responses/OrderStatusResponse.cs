namespace ScreenProducerAPI.Models.Responses;

public class OrderStatusResponse
{
    public int OrderId { get; set; }
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }
    public int TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int? AmountPaid { get; set; }
    public int RemainingBalance { get; set; }
    public bool IsFullyPaid { get; set; }
}