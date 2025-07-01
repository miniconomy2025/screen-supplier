namespace ScreenProducerAPI.Models.Requests;

public class CollectRequest
{
    public int Id { get; set; } // orderId
    public int Quantity { get; set; } // how much being picked up
}