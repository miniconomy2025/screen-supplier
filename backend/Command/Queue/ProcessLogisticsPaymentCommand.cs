using ScreenProducerAPI.Commands;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Command.Queue;

public class ProcessLogisticsPaymentCommand : ICommand<CommandResult>
{
    private readonly PurchaseOrder _purchaseOrder;
    private readonly IBankService _bankService;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILogger<ProcessLogisticsPaymentCommand> _logger;

    public ProcessLogisticsPaymentCommand(
        PurchaseOrder purchaseOrder,
        IBankService bankService,
        IPurchaseOrderService purchaseOrderService,
        ILogger<ProcessLogisticsPaymentCommand> logger)
    {
        _purchaseOrder = purchaseOrder;
        _bankService = bankService;
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
    }

    public async Task<CommandResult> ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Processing logistics payment for purchase order {PurchaseOrderId}", _purchaseOrder.Id);

            var description = $"{_purchaseOrder.ShipmentID}";

            var paymentSuccess = await _bankService.MakePaymentAsync(
                _purchaseOrder.ShipperBankAccout,
                "commercial-bank",
                _purchaseOrder.OrderShippingPrice,
                description);

            if (paymentSuccess)
            {
                await _purchaseOrderService.UpdateStatusAsync(_purchaseOrder.Id, Status.WaitingForDelivery);
                _logger.LogInformation("Logistics payment successful for purchase order {PurchaseOrderId}, amount {Amount}",
                    _purchaseOrder.Id, _purchaseOrder.OrderShippingPrice);
                return CommandResult.Succeeded();
            }
            else
            {
                _logger.LogWarning("Logistics payment failed for purchase order {PurchaseOrderId}", _purchaseOrder.Id);
                return CommandResult.Failed("Logistics payment failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing logistics payment for purchase order {PurchaseOrderId}", _purchaseOrder.Id);
            return CommandResult.Failed(ex.Message);
        }
    }
}