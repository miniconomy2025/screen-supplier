using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.ScreenDbContext;
using System.Text.Json;

namespace ScreenProducerAPI.Services.BankServices;

public class BankService(HttpClient httpClient, ScreenContext _context, IOptions<BankServiceOptions> options,
    ILogger<BankService> _logger)
{
    //TODO: This need to be tested once commercial bank is available
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<string?> GetBankAccountNumberAsync()
    {
        try
        {
            var bankDetails = await _context.BankDetails.FirstOrDefaultAsync();

            if (bankDetails == null)
            {
                _logger.LogWarning("No bank account details found in database");
                return null;
            }

            _logger.LogInformation("Retrieved bank account number: {AccountNumber}", bankDetails.AccountNumber);
            return bankDetails.AccountNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bank account number");
            return null;
        }
    }

    public async Task<bool> SetBankAccountNumberAsync(string accountNumber)
    {
        try
        {
            var existingBankDetails = await _context.BankDetails.FirstOrDefaultAsync();

            if (existingBankDetails != null)
            {
                existingBankDetails.AccountNumber = accountNumber;
                _logger.LogInformation("Updated bank account number to: {AccountNumber}", accountNumber);
            }
            else
            {
                var bankDetails = new BankDetails
                {
                    AccountNumber = accountNumber
                };
                _context.BankDetails.Add(bankDetails);
                _logger.LogInformation("Created new bank account entry: {AccountNumber}", accountNumber);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting bank account number: {AccountNumber}", accountNumber);
            return false;
        }
    }

    public async Task<bool> HasBankAccountConfiguredAsync()
    {
        try
        {
            var bankDetails = await _context.BankDetails.FirstOrDefaultAsync();
            return bankDetails != null && !string.IsNullOrEmpty(bankDetails.AccountNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if bank account is configured");
            return false;
        }
    }

    public async Task<BankAccountResponse> GetAccountNumber()
    {
        try
        {
            var baseUrl = options?.Value.BaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/account/me");

            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

            var response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var accountResponse = await response.Content.ReadFromJsonAsync<BankAccountResponse>(jsonSerializerOptions);

            await SetBankAccountNumberAsync(accountResponse.AccountNumber);

            return accountResponse == null ? throw new Exception("Failed to retrieve account number.") : accountResponse;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("An error occurred while making the payment.", ex);
        }
    }

    public async Task<BankAccountBalanceResponse> GetAccountBalance()
    {
        try
        {
            var baseUrl = options?.Value.BaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/account/me/balance");

            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

            var response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var accountBalanceResponse = await response.Content.ReadFromJsonAsync<BankAccountBalanceResponse>(jsonSerializerOptions);

            return accountBalanceResponse ?? throw new Exception("Failed to retrieve account balance.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("An error occurred while retrieving the account balance.", ex);
        }
    }

    public async Task<bool> MakePayment(MakePaymentRequest request)
    {
        try
        {
            var baseUrl = options?.Value.BaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/transaction");

            var response = await httpClient.PostAsJsonAsync(uriBuilder.Uri, request);

            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("An error occurred while making the payment.", ex);
        }
    }

    public async Task<LoanInformationResponse> GetAllLoanInformation()
    {
        try
        {
            var baseUrl = options?.Value.BaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/loan");

            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

            var response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var loanInformationResponse = await response.Content.ReadFromJsonAsync<LoanInformationResponse>(jsonSerializerOptions);

            return loanInformationResponse ?? throw new Exception("Failed to retrieve loan information.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("An error occurred while retrieving the loan information.", ex);
        }
    }

    public async Task<Loan> GetLoanInformation(string loanNumber)
    {
        try
        {
            var baseUrl = options?.Value.BaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/loan/{loanNumber}");

            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

            var response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var loanResponse = await response.Content.ReadFromJsonAsync<Loan>();

            return loanResponse ?? throw new Exception("Failed to retrieve loan.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("An error occurred while retrieving the loan.", ex);
        }
    }

    public async Task<RepayLoanResponse> PayOffLoan(RepayLoanRequest request, string loanNumber)
    {
        try
        {
            var baseUrl = options?.Value.BaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/loan/{loanNumber}/pay");

            var response = await httpClient.PostAsJsonAsync(uriBuilder.Uri, request);

            response.EnsureSuccessStatusCode();

            var loanRepayResponse = await response.Content.ReadFromJsonAsync<RepayLoanResponse>(jsonSerializerOptions);

            return loanRepayResponse ?? throw new Exception("Failed to repay the loan.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("An error occurred while repaying the loan.", ex);
        }
    }
}
