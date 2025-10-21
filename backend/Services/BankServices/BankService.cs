using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.ScreenDbContext;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenProducerAPI.Services.BankServices;

public class BankService : IBankService
{
    private readonly HttpClient _httpClient;
    private readonly ScreenContext _context;
    private readonly IOptions<BankServiceOptions> _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IBankService> _logger;
    private readonly IHandService _handService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BankService(
        HttpClient httpClient,
        ScreenContext context,
        IOptions<BankServiceOptions> options,
        IConfiguration configuration,
        ILogger<BankService> logger,
        IHandService handService)
    {
        _httpClient = httpClient;
        _context = context;
        _options = options;
        _configuration = configuration;
        _logger = logger;
        _handService = handService;
    }

    public async Task<bool> InitializeBankAccountAsync()
    {
        try
        {
            var existingAccount = await _context.BankDetails.FirstOrDefaultAsync();
            if (existingAccount != null)
            {
                return true;
            }

            var liveAccount = await GetLiveAccountInformation();
            if (liveAccount != null)
            {
                var details = new BankDetails
                {
                    AccountNumber = liveAccount.AccountNumber,
                    EstimatedBalance = liveAccount.Balance
                };


                _context.BankDetails.Add(details);
                await _context.SaveChangesAsync();
                return true;
            }

            var accountResponse = await CreateBankAccountAsync();
            if (string.IsNullOrEmpty(accountResponse?.AccountNumber))
            {
                throw new BankServiceException("Failed to create bank account - no account number returned");
            }

            var bankDetails = new BankDetails
            {
                AccountNumber = accountResponse.AccountNumber,
                EstimatedBalance = 0
            };
            _context.BankDetails.Add(bankDetails);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (BankServiceException)
        {
            throw; // Re-throw service exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize bank account");
            throw new BankServiceException("Bank account initialization failed", ex);
        }
    }

    public async Task<bool> TakeInitialLoanAsync(int initialLoanAmount)
    {
        try
        {

            var existingLoans = await GetLoansOutstanding();
            if (existingLoans != null &&
            existingLoans.loans
                .Select(x => x.InitialAmount)
                .Sum() > 0)
            {
                return true;
            }

            const int minimumLoanAmount = 500;
            const decimal decreasePercentage = 0.75m; // 25% decrease each retry

            var currentAttemptAmount = initialLoanAmount;

            while (currentAttemptAmount >= minimumLoanAmount)
            {
                _logger.LogInformation("Attempting loan for amount: {Amount}", currentAttemptAmount);

                var loanRequest = new
                {
                    amount = currentAttemptAmount
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_options.Value.BaseUrl}/loan",
                    loanRequest,
                    _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    var loanResponse = await response.Content.ReadFromJsonAsync<LoanCreationResponse>(_jsonOptions);
                    if (loanResponse?.Success == true)
                    {
                        _logger.LogInformation("Loan successful for amount: {Amount}", currentAttemptAmount);
                        var LocalAccount = await _context.BankDetails.FirstAsync();
                        LocalAccount.EstimatedBalance = LocalAccount.EstimatedBalance + loanRequest.amount;
                        _logger.LogInformation("Local account added: {Amount}", LocalAccount.EstimatedBalance);
                        await _context.SaveChangesAsync();
                        return true;
                    }
                }

                // Decrease amount for next attempt
                currentAttemptAmount = (int)(currentAttemptAmount * decreasePercentage);
                _logger.LogWarning("Loan failed, retrying with amount: {Amount}", currentAttemptAmount);
            }

            _logger.LogError("Failed to secure minimum loan amount of {MinAmount}", minimumLoanAmount);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during loan process");
            return false;
        }
    }
    public async Task<int> GetSafetyBalance()
    {
        try
        {
            var existingLoans = await GetLoansOutstanding();
            if (existingLoans?.loans != null)
            {
                decimal amountLoaned = existingLoans.loans
                    .Select(x => x.InitialAmount)
                    .Sum();

                if (amountLoaned > 0)
                {
                    return (int)Math.Ceiling(0.05m * amountLoaned);
                }
            }
            return 2000;
        }
        catch
        {
            return 2000; // Fallback safety amount
        }
    }

    public async Task<bool> HasSufficientBalanceAsync(int requiredAmount)
    {
        var localAccount = await _context.BankDetails.FirstAsync();
        try
        {
            var currentBalance = await GetAccountBalanceAsync();

            if (currentBalance != localAccount.EstimatedBalance)
            {
                localAccount.EstimatedBalance = currentBalance;
                await _context.SaveChangesAsync();
            }

            var safetyBalance = await GetSafetyBalance();
            var availableBalance = currentBalance - safetyBalance;

            return availableBalance >= requiredAmount;
        }
        catch (Exception ex)
        {
            return (localAccount.EstimatedBalance - 2000 >= requiredAmount);
        }
    }

    private async Task<BankAccountResponse?> CreateBankAccountAsync()
    {
        try
        {
            var accountCreationRequest = new
            {
                notification_url = "https://todosecuritylevelup.com/payment"
            };

            var response = await _httpClient.PostAsJsonAsync($"{_options.Value.BaseUrl}/account",
                accountCreationRequest, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new BankServiceException($"Account creation failed: {response.StatusCode} - {errorContent}");
            }

            return await response.Content.ReadFromJsonAsync<BankAccountResponse>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            throw new BankServiceException("Bank service unavailable for account creation", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new BankServiceException("Bank service timeout during account creation", ex);
        }
    }

    public async Task<bool> SetupNotificationUrlAsync()
    {
        try
        {
            var notificationUrl = _configuration["BankSettings:NotificationUrl"];
            if (string.IsNullOrEmpty(notificationUrl))
            {
                throw new BankServiceException("Notification URL not configured");
            }

            var request = new
            {
                notification_url = notificationUrl
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.Value.BaseUrl}/account/me/notify",
                request,
                _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new BankServiceException($"Notification setup failed: {response.StatusCode} - {errorContent}");
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            throw new BankServiceException("Bank service unavailable for notification setup");
        }
        catch (TaskCanceledException ex)
        {
            throw new BankServiceException("Bank service timeout during notification setup", ex);
        }
    }

    public async Task<string?> GetBankAccountNumberAsync()
    {
        try
        {
            var bankDetails = await _context.BankDetails.FirstOrDefaultAsync();
            return bankDetails?.AccountNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bank account number");
            return null;
        }
    }

    public async Task<int> GetAccountBalanceAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_options.Value.BaseUrl}/account/me/balance");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new BankServiceException($"Balance retrieval failed: {response.StatusCode} - {errorContent}");
            }

            var balanceResponse = await response.Content.ReadFromJsonAsync<BankAccountBalanceResponse>(_jsonOptions);
            return balanceResponse?.Balance ?? 0;
        }
        catch (HttpRequestException ex)
        {
            throw new BankServiceException("Bank service unavailable for balance check", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new BankServiceException("Bank service timeout during balance check", ex);
        }
    }

    public async Task<BankAccountBalanceResponse?> GetLiveAccountInformation()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_options.Value.BaseUrl}/account/me");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var balanceResponse = await response.Content.ReadFromJsonAsync<BankAccountBalanceResponse>(_jsonOptions);

            if (balanceResponse == null || string.IsNullOrEmpty(balanceResponse.AccountNumber))
            {
                return null;
            }

            var balance = await GetAccountBalanceAsync();

            balanceResponse.Balance = balance;

            return balanceResponse;
        }
        catch (HttpRequestException ex)
        {
            throw new BankServiceException("Bank service unavailable for balance check", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new BankServiceException("Bank service timeout during balance check", ex);
        }
    }

    public async Task<bool> MakePaymentAsync(string toAccountNumber, string toBankName, int amount, string description)
    {
        try
        {
            if (amount <= 0)
            {
                amount = 1;
            }

            if (toAccountNumber == "TREASURY_ACCOUNT")
            {
                toBankName = "thoh";
                toAccountNumber = "";
            }

            var paymentRequest = new
            {
                to_account_number = toAccountNumber,
                to_bank_name = toBankName,
                amount = amount,
                description = description
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.Value.BaseUrl}/transaction",
                paymentRequest,
                _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Check for insufficient funds specifically
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                    errorContent.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InsufficientFundsException(amount, await GetAccountBalanceAsync());
                }

                throw new BankServiceException($"Payment failed: {response.StatusCode} - {errorContent}");
            }

            var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>(_jsonOptions);

            var LocalAccount = await _context.BankDetails.FirstAsync();
            LocalAccount.EstimatedBalance -= amount;
            await _context.SaveChangesAsync();

            return paymentResponse?.Success == true;
        }
        catch (InsufficientFundsException)
        {
            throw; // Re-throw business exceptions
        }
        catch (HttpRequestException ex)
        {
            throw new BankServiceException($"Bank service unavailable for payment to {toAccountNumber}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new BankServiceException($"Bank service timeout during payment to {toAccountNumber}", ex);
        }
    }

    public async Task<bool> TryInitializeBankAccountAsync()
    {
        try
        {
            await InitializeBankAccountAsync();
            return true;
        }
        catch (BankServiceException ex)
        {
            _logger.LogWarning("Bank account initialization failed: {Message}. Simulation will continue with degraded functionality.", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during bank account initialization");
            return false;
        }
    }

    public async Task<bool> TryTakeInitialLoanAsync()
    {
        try
        {
            var initialLoanAmount = _configuration.GetValue<int>("BankSettings:InitialLoanAmount", 100000);

            try
            {
                var sandTarget = _configuration.GetValue<int>("TargetQuantities:Sand:target", 1000);
                var copperTarget = _configuration.GetValue<int>("TargetQuantities:Copper:target", 1000);
                var equipmentTarget = _configuration.GetValue<int>("TargetQuantities:Equipment:target", 1000);

                var machinesResponse = await _handService.GetMachinesForSaleAsync();

                var screenMachine = machinesResponse.Machines.FirstOrDefault(m => m.MachineName == "screen_machine");

                var handMaterials = await _handService.GetRawMaterialsForSaleAsync();
                var sandMaterial = handMaterials?.FirstOrDefault(m => m.RawMaterialName.Equals("sand", StringComparison.OrdinalIgnoreCase));
                var copperMaterial = handMaterials?.FirstOrDefault(m => m.RawMaterialName.Equals("copper", StringComparison.OrdinalIgnoreCase));

                if (screenMachine != null && sandMaterial != null && copperMaterial != null)
                {
                    initialLoanAmount = 2 * (int)Math.Ceiling((screenMachine.Price * equipmentTarget) + (sandMaterial.PricePerKg * sandTarget) + (copperMaterial.PricePerKg * copperTarget));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate initial loan amount based on targets. Using default value.");
            }

            return await TakeInitialLoanAsync(initialLoanAmount);
        }
        catch (BankServiceException ex)
        {
            _logger.LogWarning("Initial loan failed: {Message}. Simulation will continue with limited funds.", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during initial loan");
            return false;
        }
    }

    public async Task<bool> TrySetupNotificationUrlAsync()
    {
        try
        {
            return await SetupNotificationUrlAsync();
        }
        catch (BankServiceException ex)
        {
            _logger.LogWarning("Notification URL setup failed: {Message}. Payment notifications may not work.", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during notification URL setup");
            return false;
        }
    }

    public async Task<BankAccountLoanResponse?> GetLoansOutstanding()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_options.Value.BaseUrl}/loan");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var loanResponse = await response.Content.ReadFromJsonAsync<BankAccountLoanResponse>(_jsonOptions);
            return loanResponse;
        }
        catch (HttpRequestException ex)
        {
            throw new BankServiceException("Bank service unavailable for loans check", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new BankServiceException("Bank service timeout during loans check", ex);
        }
    }
}

public class LoanCreationResponse
{
    public bool Success { get; set; }
    [JsonPropertyName("loan_number")]
    public string LoanNumber { get; set; } = string.Empty;
}

public class PaymentResponse
{
    public bool Success { get; set; }
    [JsonPropertyName("transaction_number")]
    public string TransactionNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}