using System.Collections.Generic;

namespace ScreenProducerAPI.Services.SupplierService.Recycler.Models;

public class RecyclerOrderRequest
{
    public string CompanyName { get; set; }
    public List<RecyclerOrderItem> Items { get; set; } = [];
}