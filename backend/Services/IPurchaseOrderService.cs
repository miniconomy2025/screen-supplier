using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.Services;

public interface IPurchaseOrderService
{
    Task<PurchaseOrder?> CreatePurchaseOrderAsync(
        int orderId,
        int quantity,
        int unitPrice,
        string sellerBankAccount,
        string origin,
        int? rawMaterialId = null,
        bool isEquipmentOrder = false);
    
    Task<PurchaseOrder?> FindPurchaseOrderByShipmentIdAsync(int shipmentId);
    Task<bool> UpdateShipmentIdAsync(int purchaseOrderId, int shipmentId);
    Task<bool> UpdateDeliveryQuantityAsync(int purchaseOrderId, int deliveredQuantity);
    Task<bool> UpdateStatusAsync(int purchaseOrderId, string statusName);
    Task<bool> UpdateOrderShippingDetailsAsync(int purchaseOrderId, string bankAccount, int shippingPrice);
    Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int purchaseOrderId);
    Task<List<PurchaseOrder>> GetActivePurchaseOrdersAsync();
    Task<List<PurchaseOrder>> GetOrdersAsync();
    Task<List<PurchaseOrder>> GetPastOrdersAsync(DateTime dateTime);
}