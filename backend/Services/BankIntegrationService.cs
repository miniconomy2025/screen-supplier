using ScreenProducerAPI.Services.BankServices;

namespace ScreenProducerAPI.Services;

public class BankIntegrationService
{
    private readonly BankService _bankService;

    public BankIntegrationService(
        BankService bankService)
    {
        _bankService = bankService;
    }

    public async Task<(bool,bool,bool)> InitializeAsync(bool hasAccount, bool hasLoan, bool hasNotificationUrl)
    {
        bool loanTaken = false;
        bool accountCreated = false;
        bool notifcationUrlSet = false;

        if (!hasAccount)
        {
            accountCreated = await _bankService.TryInitializeBankAccountAsync();
        }

        if (!hasLoan)
        {
            loanTaken = await _bankService.TryTakeInitialLoanAsync();
        }

        if (!hasNotificationUrl)
        {
            notifcationUrlSet = await _bankService.TrySetupNotificationUrlAsync();
        }

        return (accountCreated, loanTaken, notifcationUrlSet);
    }
}