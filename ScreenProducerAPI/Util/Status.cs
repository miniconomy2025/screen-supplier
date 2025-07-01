namespace ScreenProducerAPI.Util;

public static class Status
{
    public static readonly string WaitingForCollection = "waiting_collection";
    public static readonly string WaitingForPayment = "waiting_payment";
    public static readonly string Collected = "collected";
    public static readonly string WaitingForDelivery = "waiting_delivery";
    public static readonly string Delivered = "delivered";
    public static readonly string Abandoned = "abandoned";
}
