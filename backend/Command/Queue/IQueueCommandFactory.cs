using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Commands.Queue;

public interface IQueueCommandFactory
{
    ICommand<CommandResult> CreateCommand(PurchaseOrder purchaseOrder);
}