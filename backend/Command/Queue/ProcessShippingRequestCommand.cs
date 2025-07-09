using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Commands.Queue;

public class ProcessShippingRequestCommand : ICommand<CommandResult>
{
    private readonly PurchaseOrder _purchaseOrder;
    private readonly LogisticsService _logisticsService;
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly EquipmentService _equipmentService;
    private readonly CompanyInfoConfig _companyInfo;
    private readonly ILogger<ProcessShippingRequestCommand> _logger;

    public ProcessShippingRequestCommand(
        PurchaseOrder purchaseOrder,
        LogisticsService logisticsService,
        PurchaseOrderService purchaseOrderService,
        EquipmentService equipmentService,
        IOptionsMonitor<CompanyInfoConfig> companyConfig,
        ILogger<ProcessShippingRequestCommand> logger)
    {
        _purchaseOrder = purchaseOrder;
        _logisticsService = logisticsService;
        _purchaseOrderService = purchaseOrderService;
        _equipmentService = equipmentService;
        _companyInfo = companyConfig.CurrentValue;
        _logger = logger;
    }

    public async Task<CommandResult> ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Processing shipping request for purchase order {PurchaseOrderId}", _purchaseOrder.Id);

            var pickupItems = await CreatePickupItemsAsync();
            if (pickupItems == null)
            {
                return CommandResult.FailedNoRetry("Invalid purchase order configuration");
            }

            var (pickupRequestId, logisticsBankAccount, shippingPrice) = await _logisticsService.RequestPickupAsync(
                _purchaseOrder.Origin,
                _companyInfo.CompanyId,
                _purchaseOrder.OrderID.ToString(),
                pickupItems
            );

            await _purchaseOrderService.UpdateShipmentIdAsync(_purchaseOrder.Id, int.Parse(pickupRequestId));
            await _purchaseOrderService.UpdateStatusAsync(_purchaseOrder.Id, Status.RequiresPaymentToLogistics);
            await _purchaseOrderService.UpdateOrderShippingDetailsAsync(_purchaseOrder.Id, logisticsBankAccount, shippingPrice);

            _logger.LogInformation("Shipping request successful for purchase order {PurchaseOrderId}, pickup request {PickupRequestId}",
                _purchaseOrder.Id, pickupRequestId);

            return CommandResult.Succeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing shipping request for purchase order {PurchaseOrderId}", _purchaseOrder.Id);
            return CommandResult.Failed(ex.Message);
        }
    }

    private async Task<List<PickupRequestItem>?> CreatePickupItemsAsync()
    {
        var equipmentParams = await _equipmentService.GetEquipmentParametersAsync();
        if (equipmentParams == null)
        {
            _logger.LogError("Failed to get equipment parameters for shipping request");
            return null;
        }

        if (_purchaseOrder.EquipmentOrder == true)
        {
            return LogisticsService.CreatePickupItems("equipment", equipmentParams.EquipmentWeight, true);
        }
        else if (_purchaseOrder.RawMaterial != null)
        {
            return LogisticsService.CreatePickupItems(_purchaseOrder.RawMaterial.Name, _purchaseOrder.Quantity, false);
        }

        return null;
    }
}