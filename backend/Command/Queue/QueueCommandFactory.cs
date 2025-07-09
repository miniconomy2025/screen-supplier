using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Commands.Queue;

public class QueueCommandFactory : IQueueCommandFactory
{
    private readonly IServiceProvider _serviceProvider;

    public QueueCommandFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICommand<CommandResult> CreateCommand(PurchaseOrder purchaseOrder)
    {
        return purchaseOrder.OrderStatus.Status switch
        {
            Status.RequiresPaymentToSupplier => new ProcessSupplierPaymentCommand(
                purchaseOrder,
                _serviceProvider.GetRequiredService<BankService>(),
                _serviceProvider.GetRequiredService<PurchaseOrderService>(),
                _serviceProvider.GetRequiredService<ILogger<ProcessSupplierPaymentCommand>>()),

            Status.RequiresDelivery => new ProcessShippingRequestCommand(
                purchaseOrder,
                _serviceProvider.GetRequiredService<LogisticsService>(),
                _serviceProvider.GetRequiredService<PurchaseOrderService>(),
                _serviceProvider.GetRequiredService<EquipmentService>(),
                _serviceProvider.GetRequiredService<IOptionsMonitor<CompanyInfoConfig>>(),
                _serviceProvider.GetRequiredService<ILogger<ProcessShippingRequestCommand>>()),

            Status.RequiresPaymentToLogistics => new ProcessLogisticsPaymentCommand(
                purchaseOrder,
                _serviceProvider.GetRequiredService<BankService>(),
                _serviceProvider.GetRequiredService<PurchaseOrderService>(),
                _serviceProvider.GetRequiredService<ILogger<ProcessLogisticsPaymentCommand>>()),

            _ => new NoOpCommand(purchaseOrder.OrderStatus.Status)
        };
    }
}