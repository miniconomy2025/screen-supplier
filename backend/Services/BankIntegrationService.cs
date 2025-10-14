using ScreenProducerAPI.Services.BankServices;

namespace ScreenProducerAPI.Services;

public class BankIntegrationService
{
    private readonly IBankService _bankService;

    public BankIntegrationService(
        IBankService bankService)
    {
        _bankService = bankService;
    }

    public async Task<(bool,bool,bool)> InitializeAsync(bool hasAccount, bool hasLoan, bool hasNotificationUrl)
    {
        bool loanTaken = hasLoan;
        bool accountCreated = hasAccount;
        bool notifcationUrlSet = hasNotificationUrl;

        if (!hasAccount)
        {
            accountCreated = await _bankService.TryInitializeBankAccountAsync();
        }

        if (!hasLoan && accountCreated)
        {
            loanTaken = await _bankService.TryTakeInitialLoanAsync();
        }

        //if (!hasNotificationUrl)
        //{
        //    notifcationUrlSet = await _bankService.TrySetupNotificationUrlAsync();
        //}

        return (accountCreated, loanTaken, true);
    }
}