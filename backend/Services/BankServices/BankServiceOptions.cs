using Microsoft.Extensions.Options;

namespace ScreenProducerAPI.Services.BankServices;

public class BankServiceOptions : IOptions<BankServiceOptions>
{
    public string BaseUrl { get; set; }
    public static string Section { get; } = "CommercialBank";
    public BankServiceOptions Value => this;
}
