namespace ScreenProducerAPI.Models;

public class QueueItem
{
    public int PurchaseOrderId { get; set; }
    public int RetryCount { get; set; }
    public DateTime LastProcessed { get; set; }
    public string LastError { get; set; } = string.Empty;

    public QueueItem(int purchaseOrderId)
    {
        PurchaseOrderId = purchaseOrderId;
        RetryCount = 0;
        LastProcessed = DateTime.UtcNow;
    }
}