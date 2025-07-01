using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;

namespace ScreenProducerAPI.Services;

public class PurchaseOrderService
{
    private readonly ScreenContext _context;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(ScreenContext context, ILogger<PurchaseOrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PurchaseOrder?> CreatePurchaseOrderAsync(
        int orderId,
        int quantity,
        int unitPrice,
        string sellerBankAccount,
        string origin,
        int? rawMaterialId = null,
        bool isEquipmentOrder = false)
    {
        try
        {
            // Get waiting_delivery status
            var waitingDeliveryStatus = await _context.OrderStatuses
                .FirstOrDefaultAsync(os => os.Status == "waiting_delivery");

            if (waitingDeliveryStatus == null)
            {
                _logger.LogError("Status 'waiting_delivery' not found");
                return null;
            }

            var purchaseOrder = new PurchaseOrder
            {
                OrderID = orderId,
                ShipmentID = null, // Will be set when pickup is requested
                Quantity = quantity,
                QuantityDelivered = 0,
                OrderDate = DateTime.UtcNow,
                UnitPrice = unitPrice,
                BankAccountNumber = sellerBankAccount,
                Origin = origin,
                OrderStatusId = waitingDeliveryStatus.Id,
                RawMaterialId = isEquipmentOrder ? null : rawMaterialId,
                EquipmentOrder = isEquipmentOrder
            };

            _context.PurchaseOrders.Add(purchaseOrder);
            await _context.SaveChangesAsync();

            var orderType = isEquipmentOrder ? "equipment" : "material";
            _logger.LogInformation("Created {OrderType} purchase order {PurchaseOrderId} for order {OrderId}: {Quantity} units at {UnitPrice} each",
                orderType, purchaseOrder.Id, orderId, quantity, unitPrice);

            return purchaseOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase order for order {OrderId}", orderId);
            return null;
        }
    }

    public async Task<PurchaseOrder?> FindPurchaseOrderByShipmentIdAsync(int shipmentId)
    {
        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.OrderStatus)
                .Include(po => po.RawMaterial)
                .FirstOrDefaultAsync(po => po.ShipmentID == shipmentId);

            return purchaseOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding purchase order by shipment ID {ShipmentId}", shipmentId);
            return null;
        }
    }

    public async Task<bool> UpdateShipmentIdAsync(int purchaseOrderId, int shipmentId)
    {
        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

            if (purchaseOrder == null)
            {
                _logger.LogWarning("Purchase order {PurchaseOrderId} not found", purchaseOrderId);
                return false;
            }

            purchaseOrder.ShipmentID = shipmentId;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated purchase order {PurchaseOrderId} with shipment ID {ShipmentId}",
                purchaseOrderId, shipmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase order {PurchaseOrderId} with shipment ID", purchaseOrderId);
            return false;
        }
    }

    public async Task<bool> UpdateDeliveryQuantityAsync(int purchaseOrderId, int deliveredQuantity)
    {
        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

            if (purchaseOrder == null)
            {
                _logger.LogWarning("Purchase order {PurchaseOrderId} not found", purchaseOrderId);
                return false;
            }

            purchaseOrder.QuantityDelivered += deliveredQuantity;

            // Check if fully delivered
            if (purchaseOrder.QuantityDelivered >= purchaseOrder.Quantity)
            {
                var deliveredStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(os => os.Status == "delivered");

                if (deliveredStatus != null)
                {
                    purchaseOrder.OrderStatusId = deliveredStatus.Id;
                    _logger.LogInformation("Purchase order {PurchaseOrderId} marked as fully delivered", purchaseOrderId);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated purchase order {PurchaseOrderId} delivered quantity: +{DeliveredQuantity}, total: {TotalDelivered}/{TotalOrdered}",
                purchaseOrderId, deliveredQuantity, purchaseOrder.QuantityDelivered, purchaseOrder.Quantity);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery quantity for purchase order {PurchaseOrderId}", purchaseOrderId);
            return false;
        }
    }

    public async Task<bool> UpdateStatusAsync(int purchaseOrderId, string statusName)
    {
        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

            if (purchaseOrder == null)
            {
                _logger.LogWarning("Purchase order {PurchaseOrderId} not found", purchaseOrderId);
                return false;
            }

            var status = await _context.OrderStatuses
                .FirstOrDefaultAsync(os => os.Status == statusName);

            if (status == null)
            {
                _logger.LogWarning("Status '{StatusName}' not found", statusName);
                return false;
            }

            purchaseOrder.OrderStatusId = status.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated purchase order {PurchaseOrderId} status to '{StatusName}'",
                purchaseOrderId, statusName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase order {PurchaseOrderId} status", purchaseOrderId);
            return false;
        }
    }

    public async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int purchaseOrderId)
    {
        return await _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .Include(po => po.RawMaterial)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);
    }

    public async Task<List<PurchaseOrder>> GetActivePurchaseOrdersAsync()
    {
        return await _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .Include(po => po.RawMaterial)
            .Where(po => po.OrderStatus.Status != "delivered")
            .ToListAsync();
    }
}