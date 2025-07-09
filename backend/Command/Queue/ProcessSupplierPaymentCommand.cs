using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Commands.Queue;

public class ProcessSupplierPaymentCommand : ICommand<CommandResult>
{
    private readonly PurchaseOrder _purchaseOrder;
    private readonly BankService _bankService;
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly ILogger<ProcessSupplierPaymentCommand> _logger;

    public ProcessSupplierPaymentCommand(
        PurchaseOrder purchaseOrder,
        BankService bankService,
        PurchaseOrderService purchaseOrderService,
        ILogger<ProcessSupplierPaymentCommand> logger)
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
            _logger.LogInformation("Processing supplier payment for purchase order {PurchaseOrderId}", _purchaseOrder.Id);

            var totalAmount = _purchaseOrder.Quantity * _purchaseOrder.UnitPrice;
            var description = _purchaseOrder.OrderID.ToString();

            var paymentSuccess = await _bankService.MakePaymentAsync(
                _purchaseOrder.BankAccountNumber,
                "commercial-bank",
                totalAmount,
                description);

            if (paymentSuccess)
            {
                await _purchaseOrderService.UpdateStatusAsync(_purchaseOrder.Id, Status.RequiresDelivery);
                _logger.LogInformation("Supplier payment successful for purchase order {PurchaseOrderId}, amount {Amount}",
                    _purchaseOrder.Id, totalAmount);
                return CommandResult.Succeeded();
            }
            else
            {
                _logger.LogWarning("Supplier payment failed for purchase order {PurchaseOrderId}", _purchaseOrder.Id);
                return CommandResult.Failed("Supplier payment failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing supplier payment for purchase order {PurchaseOrderId}", _purchaseOrder.Id);
            return CommandResult.Failed(ex.Message);
        }
    }
}
