using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.ScreenDbContext;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenProducerAPI.Services.BankServices;

public class BankService
{
    private readonly HttpClient _httpClient;
    private readonly ScreenContext _context;
    private readonly IOptions<BankServiceOptions> _options;
    private readonly IConfiguration _configuration;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BankService(
        HttpClient httpClient,
        ScreenContext context,
        IOptions<BankServiceOptions> options,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _context = context;
        _options = options;
        _configuration = configuration;
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

            var accountResponse = await CreateBankAccountAsync();
            if (string.IsNullOrEmpty(accountResponse?.AccountNumber))
            {
                return false;
            }

            var bankDetails = new BankDetails
            {
                AccountNumber = accountResponse.AccountNumber
            };
            _context.BankDetails.Add(bankDetails);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> TakeInitialLoanAsync()
    {
        try
        {
            var loanAmount = _configuration.GetValue<int>("BankSettings:InitialLoanAmount", 50000);

            var loanRequest = new
            {
                amount = loanAmount
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.Value.BaseUrl}/loan",
                loanRequest,
                _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return false;
            }

            var loanResponse = await response.Content.ReadFromJsonAsync<LoanCreationResponse>(_jsonOptions);
            if (loanResponse?.Success == true)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private async Task<BankAccountResponse?> CreateBankAccountAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_options.Value.BaseUrl}/account",
                JsonContent.Create(new { }));
            var rawJson = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BankAccountResponse>(_jsonOptions);
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<bool> SetupNotificationUrlAsync()
    {
        try
        {
            var notificationUrl = _configuration["BankSettings:NotificationUrl"];
            if (string.IsNullOrEmpty(notificationUrl))
            {
                return false;
            }

            var request = new
            {
                notification_url = notificationUrl
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.Value.BaseUrl}/account/me/notify",
                request,
                _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;

        }
        catch (Exception ex)
        {
            return false;
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
            return null;
        }
    }

    public async Task<int> GetAccountBalanceAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_options.Value.BaseUrl}/account/me/balance");
            response.EnsureSuccessStatusCode();

            var balanceResponse = await response.Content.ReadFromJsonAsync<BankAccountBalanceResponse>(_jsonOptions);
            return balanceResponse?.Balance ?? 0;
        }
        catch (Exception ex)
        {
            return 0;
        }
    }

    public async Task<bool> MakePaymentAsync(string toAccountNumber, string toBankName, int amount, string description)
    {
        try
        {
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

            if (response.IsSuccessStatusCode)
            {
                var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>(_jsonOptions);
                if (paymentResponse?.Success == true)
                {
                    return true;
                }
            }                
        }
        catch (Exception ex)
        {
            return false;
        }
        return false;
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