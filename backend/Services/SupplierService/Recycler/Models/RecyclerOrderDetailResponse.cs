using System;
using System.Collections.Generic;

namespace ScreenProducerAPI.Services.SupplierService.Recycler.Models;

public class RecyclerOrderDetailResponse
{
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<RecyclerOrderDetailItem> Items { get; set; } = [];
}