using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Services;

public class ScreenOrderService
{
    private readonly ScreenContext _context;
    private readonly ProductService _productService;
    private readonly SimulationTimeProvider _simulationTimeProvider;

    public ScreenOrderService(ScreenContext context, ILogger<ScreenOrderService> logger, ProductService productService, SimulationTimeProvider simulationTimeProvider)
    {
        _context = context;
        _productService = productService;
        _simulationTimeProvider = simulationTimeProvider;
    }

    public async Task<PaymentConfirmationResponse?> ProcessPaymentConfirmationAsync(PaymentConfirmationRequest request)
    {
        try
        {
            if (!int.TryParse(request.ReferenceId, out int orderId))
            {
                return new PaymentConfirmationResponse
                {
                    Success = false,
                    OrderId = request.ReferenceId,
                    Message = "Invalid reference ID format",
                    ProcessedAt = _simulationTimeProvider.Now
                };
            }

            var screenOrder = await _context.ScreenOrders
                .Include(so => so.OrderStatus)
                .Include(so => so.Product)
                .FirstOrDefaultAsync(so => so.Id == orderId && so.OrderStatus.Status == Status.WaitingForPayment);

            if (screenOrder == null)
            {
                return new PaymentConfirmationResponse
                {
                    Success = false,
                    OrderId = request.ReferenceId,
                    Message = "Order not found",
                    ProcessedAt = _simulationTimeProvider.Now
                };
            }

            var orderTotal = screenOrder.Quantity * screenOrder.UnitPrice;
            var previouslyPaid = screenOrder.AmountPaid ?? 0;
            var newTotalPaid = previouslyPaid + (int)request.AmountPaid;
            var remainingBalance = Math.Max(0, orderTotal - newTotalPaid);
            var isFullyPaid = newTotalPaid >= orderTotal;

            screenOrder.AmountPaid = newTotalPaid;

            string newStatus = screenOrder.OrderStatus?.Status ?? "unknown";
            if (isFullyPaid && screenOrder.OrderStatus?.Status == Status.WaitingForPayment)
            {
                var waitingCollectionStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(os => os.Status == Status.WaitingForCollection);

                if (waitingCollectionStatus != null)
                {
                    screenOrder.OrderStatusId = waitingCollectionStatus.Id;
                    newStatus = Status.WaitingForCollection;
                }
            }

            await _context.SaveChangesAsync();

            return new PaymentConfirmationResponse
            {
                Success = true,
                OrderId = orderId.ToString(),
                AmountReceived = request.AmountPaid,
                TotalPaid = newTotalPaid,
                OrderTotal = orderTotal,
                RemainingBalance = remainingBalance,
                Status = newStatus,
                Message = isFullyPaid ? "Order fully paid and ready for collection" : $"Partial payment received. Remaining balance: {remainingBalance}",
                IsFullyPaid = isFullyPaid,
                ProcessedAt = _simulationTimeProvider.Now
            };
        }
        catch (Exception ex)
        {
            return new PaymentConfirmationResponse
            {
                Success = false,
                OrderId = request.ReferenceId,
                Message = "Internal error processing payment",
                ProcessedAt = _simulationTimeProvider.Now
            };
        }
    }

    public async Task<ScreenOrder?> CreateOrderAsync(int quantity, string? customerInfo = null)
    {
        try
        {
            var product = await _productService.GetProductAsync();
            if (product == null)
            {
                return null;
            }

            var availableStock = await _productService.GetAvailableStockAsync();
            if (availableStock < quantity)
            {
                return null;
            }

            var waitingPaymentStatus = await _context.OrderStatuses
                .FirstOrDefaultAsync(os => os.Status == Status.WaitingForPayment);

            if (waitingPaymentStatus == null)
            {
                return null;
            }

            var screenOrder = new ScreenOrder
            {
                Quantity = quantity,
                OrderDate = _simulationTimeProvider.Now,
                UnitPrice = product.Price,
                OrderStatusId = waitingPaymentStatus.Id,
                ProductId = product.Id,
                QuantityCollected = 0,
                AmountPaid = 0
            };

            _context.ScreenOrders.Add(screenOrder);
            await _context.SaveChangesAsync();

            return screenOrder;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<ScreenOrder?> FindScreenOrderByIdAsync(int orderId)
    {
        try
        {
            var screenOrder = await _context.ScreenOrders
                .Include(so => so.OrderStatus)
                .Include(so => so.Product)
                .FirstOrDefaultAsync(so => so.Id == orderId);

            return screenOrder;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<bool> UpdateStatusAsync(int orderId, string statusName)
    {
        try
        {
            var screenOrder = await _context.ScreenOrders
                .FirstOrDefaultAsync(so => so.Id == orderId);

            if (screenOrder == null)
            {
                return false;
            }

            var status = await _context.OrderStatuses
                .FirstOrDefaultAsync(os => os.Status == statusName);

            if (status == null)
            {
                return false;
            }

            screenOrder.OrderStatusId = status.Id;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> UpdatePaymentAsync(int orderId, int amountPaid)
    {
        try
        {
            var screenOrder = await _context.ScreenOrders
                .FirstOrDefaultAsync(so => so.Id == orderId);

            if (screenOrder == null)
            {
                return false;
            }

            screenOrder.AmountPaid = amountPaid;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> UpdateQuantityCollectedAsync(int purchaseOrderId, int quantityCollected)
    {
        try
        {
            var screenOrder = await _context.ScreenOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

            if (screenOrder == null)
            {
                return false;
            }

            screenOrder.QuantityCollected += quantityCollected;

            if (screenOrder.QuantityCollected >= screenOrder.Quantity)
            {
                var deliveredStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(os => os.Status == Status.Collected);

                if (deliveredStatus != null)
                {
                    screenOrder.OrderStatusId = deliveredStatus.Id;
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

    public async Task<List<ScreenOrder>> GetActiveScreenOrdersAsync()
    {
        return await _context.ScreenOrders
            .Include(so => so.OrderStatus)
            .Include(so => so.Product)
            .Where(so => so.OrderStatus.Status != Status.Collected)
            .ToListAsync();
    }

    public async Task<List<ScreenOrder>> GetOrdersByStatusAsync(string statusName)
    {
        return await _context.ScreenOrders
            .Include(so => so.OrderStatus)
            .Include(so => so.Product)
            .Where(so => so.OrderStatus.Status == statusName)
            .ToListAsync();
    }

    public async Task<List<ScreenOrder>> GetOrdersByDateAsync(DateTime date)
    {
        List<ScreenOrder> orders = [];

        foreach (var item in _context.ScreenOrders)
        {
            if (item.OrderDate == date)
            {
                orders.Add(item);
            }
        }

        return orders;
    }

    public async Task<List<ScreenOrder>> GetPastOrdersAsync(DateTime date)
    {
        var orders = await _context.ScreenOrders
            .Include(so => so.OrderStatus)
            .Include(so => so.Product)
            .OrderByDescending(po => po.OrderDate)
            .Take(100)
            .ToListAsync();

        return orders;
    }
}