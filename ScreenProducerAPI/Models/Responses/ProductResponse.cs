namespace ScreenProducerAPI.Models.Responses;

public class ProductResponse
{
    public Screens Screens { get; set; }
}

public class Screens
{
    public int Quantity { get; set; }

    public int Price { get; set; }
}
