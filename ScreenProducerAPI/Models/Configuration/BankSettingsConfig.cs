namespace ScreenProducerAPI.Models.Configuration;

public class BankSettingsConfig
{
    public int InitialLoanAmount { get; set; } = 50000;
    public string NotificationUrl { get; set; } = string.Empty;
    public int MinimumBalance { get; set; } = 5000;
    public bool EnableAutomaticLoanRepayment { get; set; } = false;
}