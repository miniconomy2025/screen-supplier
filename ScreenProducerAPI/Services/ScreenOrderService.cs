using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;

namespace ScreenProducerAPI.Services;

public class ScreenOrderService
{
    private readonly ScreenContext _context;
    private readonly ILogger<ScreenOrderService> _logger;
    private readonly ProductService _productService;

    public ScreenOrderService(ScreenContext context, ILogger<ScreenOrderService> logger, ProductService productService)
    {
        _context = context;
        _logger = logger;
        _productService = productService;
    }

    public async Task<ScreenOrder?> CreateOrderAsync(int quantity, string? customerInfo = null)
    {
        try
        {
            // Get current product and price
            var product = await _productService.GetProductAsync();
            if (product == null)
            {
                _logger.LogError("No product found to create order");
                return null;
            }

            // Check if we have enough screens available (using smart stock calculation)
            var availableStock = await _productService.GetAvailableStockAsync();
            if (availableStock < quantity)
            {
                _logger.LogWarning("Insufficient screens available. Requested: {Requested}, Available: {Available}", 
                    quantity, availableStock);
                return null;
            }

            // Get waiting_payment status
            var waitingPaymentStatus = await _context.OrderStatuses
                .FirstOrDefaultAsync(os => os.Status == "waiting_payment");

            if (waitingPaymentStatus == null)
            {
                _logger.LogError("Status 'waiting_payment' not found");
                return null;
            }

            // Create the screen order
            var screenOrder = new ScreenOrder
            {
                Quantity = quantity,
                OrderDate = DateTime.UtcNow,
                UnitPrice = product.Price,
                OrderStatusId = waitingPaymentStatus.Id,
                ProductId = product.Id,
                AmountPaid = null
            };

            _context.ScreenOrders.Add(screenOrder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created screen order {OrderId} for {Quantity} screens at {UnitPrice} each. Total: {Total}",
                screenOrder.Id, quantity, product.Price, quantity * product.Price);

            return screenOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating screen order for {Quantity} screens", quantity);
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
            _logger.LogError(ex, "Error finding screen order by ID {OrderId}", orderId);
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
                _logger.LogWarning("Screen order {OrderId} not found", orderId);
                return false;
            }

            var status = await _context.OrderStatuses
                .FirstOrDefaultAsync(os => os.Status == statusName);

            if (status == null)
            {
                _logger.LogWarning("Status '{StatusName}' not found", statusName);
                return false;
            }

            screenOrder.OrderStatusId = status.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated screen order {OrderId} status to '{StatusName}'", orderId, statusName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating screen order {OrderId} status", orderId);
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
                _logger.LogWarning("Screen order {OrderId} not found for payment update", orderId);
                return false;
            }

            screenOrder.AmountPaid = amountPaid;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated screen order {OrderId} payment amount to {AmountPaid}", orderId, amountPaid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment for screen order {OrderId}", orderId);
            return false;
        }
    }

    public async Task<List<ScreenOrder>> GetActiveScreenOrdersAsync()
    {
        return await _context.ScreenOrders
            .Include(so => so.OrderStatus)
            .Include(so => so.Product)
            .Where(so => so.OrderStatus.Status != "collected")
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
}