using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;

namespace ScreenProducerAPI.Services;

public interface IScreenOrderService
{
    Task<PaymentConfirmationResponse?> ProcessPaymentConfirmationAsync(TransactionNotification notification, string refID);
    Task<ScreenOrder> CreateOrderAsync(int quantity, string? customerInfo = null);
    Task<ScreenOrder> FindScreenOrderByIdAsync(int orderId);
    Task<bool> UpdateStatusAsync(int orderId, string statusName);
    Task<bool> UpdatePaymentAsync(int orderId, int amountPaid);
    Task<bool> UpdateQuantityCollectedAsync(int purchaseOrderId, int quantityCollected);
    Task<List<ScreenOrder>> GetActiveScreenOrdersAsync();
    Task<List<ScreenOrder>> GetOrdersByStatusAsync(string statusName);
    Task<List<ScreenOrder>> GetOrdersByDateAsync(DateTime date);
    Task<List<ScreenOrder>> GetPastOrdersAsync(DateTime date);
}