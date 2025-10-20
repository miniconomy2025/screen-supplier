using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.IntegrationTests.Mocks;

public class MockLogisticsService : ILogisticsService
{
    public Task<LogisticsResponse> HandleDropoffAsync(int quantityIn, int shipmentId)
    {
        // Simulate successful delivery
        var response = new LogisticsResponse
        {
            Success = true,
            Id = shipmentId,
            OrderId = shipmentId + 1000, // Mock order ID
            Quantity = quantityIn,
            ItemType = "sand", // Default to sand for testing
            Message = $"Successfully received {quantityIn} units of sand",
            ProcessedAt = DateTime.UtcNow
        };

        return Task.FromResult(response);
    }

    public Task<LogisticsResponse?> HandleCollectAsync(int quantity, int orderId)
    {
        if (orderId == 99999) // Special case for testing not found scenario
        {
            return Task.FromResult<LogisticsResponse?>(null);
        }

        var response = new LogisticsResponse
        {
            Success = true,
            OrderId = orderId,
            Quantity = quantity,
            ItemType = "screens",
            Message = "collected",
            ProcessedAt = DateTime.UtcNow
        };

        return Task.FromResult<LogisticsResponse?>(response);
    }

    public Task<(string PickupRequestId, string BankAccountNumber, int Price)> RequestPickupAsync(
        string originCompanyId,
        string destinationCompanyId,
        string originalExternalOrderId,
        List<PickupRequestItem> items)
    {
        // Simulate successful pickup request
        return Task.FromResult(("MOCK-PICKUP-123", "MOCK-BANK-ACCOUNT", 100));
    }
}
