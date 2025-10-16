namespace ScreenProducerAPI.Services;

public interface IBankIntegrationService
{
    Task<(bool accountCreated, bool loanTaken, bool notificationUrlSet)> InitializeAsync(bool hasAccount, bool hasLoan, bool hasNotificationUrl);
}