namespace ScreenProducerAPI.Util;

public static class Status
{
    public const string WaitingForCollection = "waiting_collection";
    public const string WaitingForPayment = "waiting_payment";
    public const string Collected = "collected";
    public const string Abandoned = "abandoned";
    public const string RequiresPaymentToSupplier = "requires_payment_supplier";
    public const string RequiresDelivery = "requires_delivery";
    public const string RequiresPaymentToLogistics = "requires_payment_delivery";
    public const string WaitingForDelivery = "waiting_delivery";
    public const string Delivered = "delivered";
}
