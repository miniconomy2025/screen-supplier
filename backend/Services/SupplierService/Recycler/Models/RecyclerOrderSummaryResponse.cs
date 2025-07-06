using System;

namespace ScreenProducerAPI.Services.SupplierService.Recycler.Models;

public class RecyclerOrderSummaryResponse
{
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}