using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services.BankServices;

namespace ScreenProducerAPI.IntegrationTests.Mocks;

public class MockBankService : IBankService
{
    private bool _accountInitialized = true;
    private int _currentBalance = 100000;
    private string _accountNumber = "TEST-ACC-12345";
    private readonly List<Loan> _loans = new();

    public Task<bool> InitializeBankAccountAsync()
    {
        _accountInitialized = true;
        return Task.FromResult(true);
    }

    public Task<bool> TakeInitialLoanAsync(int initialLoanAmount)
    {
        _currentBalance += initialLoanAmount;

        _loans.Add(new Loan
        {
            LoanNumber = "LOAN-" + Guid.NewGuid().ToString()[..8],
            InitialAmount = initialLoanAmount,
            InterestRate = 5.0,
            OutstandingAmount = initialLoanAmount,
            WriteOff = false
        });

        return Task.FromResult(true);
    }

    public Task<int> GetSafetyBalance()
    {
        return Task.FromResult(10000);
    }

    public Task<bool> HasSufficientBalanceAsync(int requiredAmount)
    {
        var safetyBalance = 10000;
        var available = _currentBalance - safetyBalance;
        return Task.FromResult(available >= requiredAmount);
    }

    public Task<bool> SetupNotificationUrlAsync()
    {
        return Task.FromResult(true);
    }

    public Task<string?> GetBankAccountNumberAsync()
    {
        return Task.FromResult<string?>(_accountInitialized ? _accountNumber : null);
    }

    public Task<int> GetAccountBalanceAsync()
    {
        return Task.FromResult(_currentBalance);
    }

    public Task<BankAccountBalanceResponse?> GetLiveAccountInformation()
    {
        if (!_accountInitialized)
            return Task.FromResult<BankAccountBalanceResponse?>(null);

        var response = new BankAccountBalanceResponse
        {
            AccountNumber = _accountNumber,
            Balance = _currentBalance
        };

        return Task.FromResult<BankAccountBalanceResponse?>(response);
    }

    public Task<bool> MakePaymentAsync(string toAccountNumber, string toBankName, int amount, string description)
    {
        if (_currentBalance < amount)
            return Task.FromResult(false);

        _currentBalance -= amount;
        return Task.FromResult(true);
    }

    public Task<bool> TryInitializeBankAccountAsync()
    {
        return InitializeBankAccountAsync();
    }

    public Task<bool> TryTakeInitialLoanAsync()
    {
        return TakeInitialLoanAsync(50000);
    }

    public Task<bool> TrySetupNotificationUrlAsync()
    {
        return SetupNotificationUrlAsync();
    }

    public Task<BankAccountLoanResponse?> GetLoansOutstanding()
    {
        var response = new BankAccountLoanResponse
        {
            Success = true,
            TotalOutstandingAmount = _loans.Sum(l => l.OutstandingAmount),
            loans = _loans
        };

        return Task.FromResult<BankAccountLoanResponse?>(response);
    }

    public Task<BankDetails> AddBankAccountAsync()
    {
        return new Task<BankDetails>(() => new BankDetails
        {
            AccountNumber = _accountNumber,
            EstimatedBalance = 0
        });
    }
}
