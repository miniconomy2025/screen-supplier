namespace ScreenProducerAPI.Models.Requests;

public class DropoffRequest
{
    public int Id { get; set; } // shipmentId
    public int Quantity { get; set; } // how much being delivered
}