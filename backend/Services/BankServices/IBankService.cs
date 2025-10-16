using ScreenProducerAPI.Models.Responses;

namespace ScreenProducerAPI.Services.BankServices;

public interface IBankService
{
    Task<bool> InitializeBankAccountAsync();
    Task<bool> TakeInitialLoanAsync(int initialLoanAmount);
    Task<int> GetSafetyBalance();
    Task<bool> HasSufficientBalanceAsync(int requiredAmount);
    Task<bool> SetupNotificationUrlAsync();
    Task<string?> GetBankAccountNumberAsync();
    Task<int> GetAccountBalanceAsync();
    Task<BankAccountBalanceResponse?> GetLiveAccountInformation();
    Task<bool> MakePaymentAsync(string toAccountNumber, string toBankName, int amount, string description);
    Task<bool> TryInitializeBankAccountAsync();
    Task<bool> TryTakeInitialLoanAsync();
    Task<bool> TrySetupNotificationUrlAsync();
    Task<BankAccountLoanResponse?> GetLoansOutstanding();
}