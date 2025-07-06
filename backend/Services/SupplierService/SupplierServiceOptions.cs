using Microsoft.Extensions.Options;

namespace ScreenProducerAPI.Services.SupplierService;

public class SupplierServiceOptions : IOptions<SupplierServiceOptions>
{
    public string HandBaseUrl { get; set; }
    public string RecyclerBaseUrl { get; set; }
    public static string Section { get; } = "Suppliers";
    public SupplierServiceOptions Value => this;
}
