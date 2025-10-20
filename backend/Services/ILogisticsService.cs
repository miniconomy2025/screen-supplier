using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;

namespace ScreenProducerAPI.Services;

public interface ILogisticsService
{
    Task<LogisticsResponse> HandleDropoffAsync(int quantityIn, int shipmentId);
    Task<LogisticsResponse?> HandleCollectAsync(int quantity, int orderId);
    Task<(string PickupRequestId, string BankAccountNumber, int Price)> RequestPickupAsync(string originCompanyId, string destinationCompanyId, string originalExternalOrderId, List<PickupRequestItem> items);
}