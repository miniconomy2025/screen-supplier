namespace ScreenProducerAPI.Services;

public interface IPurchaseOrderQueueService
{
    void EnqueuePurchaseOrder(int purchaseOrderId);
    Task ProcessQueueAsync();
    Task PopulateQueueFromDatabaseAsync();
}