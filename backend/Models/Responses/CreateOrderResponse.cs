namespace ScreenProducerAPI.Models.Responses;

public class CreateOrderResponse
{
    public int OrderId { get; set; }
    public int TotalPrice { get; set; }
    public string BankAccountNumber { get; set; } = string.Empty;
    public string OrderStatusLink { get; set; } = string.Empty;
}
