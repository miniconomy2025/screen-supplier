using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Services;

public class PurchaseOrderService
{
    private readonly ScreenContext _context;
    private readonly SimulationTimeProvider _simulationTimeProvider;

    public PurchaseOrderService(ScreenContext context, SimulationTimeProvider simulationTimeProvider)
    {
        _context = context;
        _simulationTimeProvider = simulationTimeProvider;
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
            var requiresPaymentToSupplier = await _context.OrderStatuses
                .FirstOrDefaultAsync(os => os.Status == Status.RequiresPaymentToSupplier);

            if (requiresPaymentToSupplier == null)
            {
                return null;
            }

            var purchaseOrder = new PurchaseOrder
            {
                OrderID = orderId,
                ShipmentID = null, // Will be set when pickup is requested
                Quantity = quantity,
                QuantityDelivered = 0,
                OrderDate = _simulationTimeProvider.Now,
                UnitPrice = unitPrice,
                BankAccountNumber = sellerBankAccount,
                Origin = origin,
                OrderStatusId = requiresPaymentToSupplier.Id,
                RawMaterialId = isEquipmentOrder ? null : rawMaterialId,
                EquipmentOrder = isEquipmentOrder
            };

            _context.PurchaseOrders.Add(purchaseOrder);
            await _context.SaveChangesAsync();

            var orderType = isEquipmentOrder ? "equipment" : "material";

            return purchaseOrder;
        }
        catch (Exception ex)
        {
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
                return false;
            }

            purchaseOrder.ShipmentID = shipmentId;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
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
                return false;
            }

            purchaseOrder.QuantityDelivered += deliveredQuantity;

            if (purchaseOrder.QuantityDelivered >= purchaseOrder.Quantity)
            {
                var deliveredStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(os => os.Status == Status.Delivered);

                if (deliveredStatus != null)
                {
                    purchaseOrder.OrderStatusId = deliveredStatus.Id;
                }
            }

            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
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
                return false;
            }

            var status = await _context.OrderStatuses
                .FirstOrDefaultAsync(os => os.Status == statusName);

            if (status == null)
            {
                return false;
            }

            purchaseOrder.OrderStatusId = status.Id;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> UpdateOrderShippingDetailsAsync(int purchaseOrderId, string bankAccount, int shippingPrice)
    {
        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

            if (purchaseOrder == null)
            {
                return false;
            }

            purchaseOrder.ShipperBankAccout = bankAccount;
            purchaseOrder.OrderShippingPrice = shippingPrice;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
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
            .Where(po => po.OrderStatus.Status != Status.Delivered)
            .ToListAsync();
    }

    public async Task<List<PurchaseOrder>> GetOrdersAsync()
    {
        return await _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .Include(po => po.RawMaterial)
            .ToListAsync();
    }

    public async Task<List<PurchaseOrder>> GetPastOrdersAsync(DateTime dateTime)
    {
        var orders = await _context.PurchaseOrders
            .Include(po => po.OrderStatus)
            .Include(po => po.RawMaterial)
            .OrderByDescending(po => po.OrderDate)
            .Take(100)
            .ToListAsync();

        return orders;
    }
}