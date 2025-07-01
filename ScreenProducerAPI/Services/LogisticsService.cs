using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.Services;
using System.Text.Json;
using System.Text;

namespace ScreenProducerAPI.Services;

public class LogisticsService
{
    private readonly ILogger<LogisticsService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly ScreenOrderService _screenOrderService;
    private readonly MaterialService _materialService;
    private readonly EquipmentService _equipmentService;
    private readonly ProductService _productService;

    public LogisticsService(
        ILogger<LogisticsService> logger,
        IConfiguration configuration,
        HttpClient httpClient,
        PurchaseOrderService purchaseOrderService,
        ScreenOrderService screenOrderService,
        MaterialService materialService,
        EquipmentService equipmentService,
        ProductService productService)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
        _purchaseOrderService = purchaseOrderService;
        _screenOrderService = screenOrderService;
        _materialService = materialService;
        _equipmentService = equipmentService;
        _productService = productService;
    }

    public async Task<DropoffResponse> HandleDropoffAsync(DropoffRequest request)
    {
        int shipmentId = request.Id;
        int quantity = request.Quantity;

        _logger.LogInformation("Handling dropoff for shipment {ShipmentId} with quantity {Quantity}", 
            shipmentId, quantity);

        try
        {
            // Find purchase order by shipment ID
            var purchaseOrder = await _purchaseOrderService.FindPurchaseOrderByShipmentIdAsync(shipmentId);
            if (purchaseOrder == null)
            {
                throw new InvalidOperationException($"Purchase order with shipment ID {shipmentId} not found");
            }

            // Validate delivery quantity
            var remainingQuantity = purchaseOrder.Quantity - purchaseOrder.QuantityDelivered;
            if (quantity > remainingQuantity)
            {
                throw new InvalidOperationException($"Delivery quantity {quantity} exceeds remaining order quantity {remainingQuantity}");
            }

            string itemType = "";
            bool processed = false;

            // Handle equipment delivery
            if (purchaseOrder.EquipmentOrder == true)
            {
                for (int i = 0; i < quantity; i++)
                {
                    var equipmentAdded = await _equipmentService.AddEquipmentAsync(purchaseOrder.Id);
                    if (!equipmentAdded)
                    {
                        throw new InvalidOperationException($"Failed to add equipment #{i + 1} for purchase order {purchaseOrder.Id}");
                    }
                }
                itemType = "equipment";
                processed = true;
                _logger.LogInformation("Added {Quantity} equipment units for purchase order {PurchaseOrderId}", 
                    quantity, purchaseOrder.Id);
            }
            // Handle material delivery
            else if (purchaseOrder.RawMaterialId.HasValue && purchaseOrder.RawMaterial != null)
            {
                var materialName = purchaseOrder.RawMaterial.Name;
                processed = await _materialService.AddMaterialAsync(materialName, quantity);
                itemType = materialName;
                _logger.LogInformation("Added {Quantity}kg of {MaterialName} for purchase order {PurchaseOrderId}", 
                    quantity, materialName, purchaseOrder.Id);
            }
            else
            {
                throw new InvalidOperationException($"Purchase order {purchaseOrder.Id} has invalid configuration");
            }

            if (!processed)
            {
                throw new InvalidOperationException($"Failed to process {itemType} delivery");
            }

            // Update delivery quantity
            await _purchaseOrderService.UpdateDeliveryQuantityAsync(purchaseOrder.Id, quantity);

            return new DropoffResponse
            {
                Success = true,
                ShipmentId = shipmentId,
                OrderId = purchaseOrder.OrderID,
                QuantityReceived = quantity,
                ItemType = itemType,
                Message = $"Successfully received {quantity} units of {itemType}",
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling dropoff for shipment {ShipmentId}", shipmentId);
            throw;
        }
    }

    public async Task<CollectResponse?> HandleCollectAsync(CollectRequest request)
    {
        int orderId = request.Id;
        int quantity = request.Quantity;

        _logger.LogInformation("Handling collect for order {OrderId} with quantity {Quantity}", 
            orderId, quantity);

        try
        {
            // Find screen order by ID
            var screenOrder = await _screenOrderService.FindScreenOrderByIdAsync(orderId);
            if (screenOrder == null)
            {
                _logger.LogWarning("Screen order {OrderId} not found", orderId);
                return null;
            }

            // Check if order is ready for collection
            if (screenOrder.OrderStatus?.Status != "waiting_collection")
            {
                throw new InvalidOperationException($"Screen order {orderId} is not ready for collection. Current status: {screenOrder.OrderStatus?.Status}");
            }

            // Validate collection quantity
            if (quantity > screenOrder.Quantity)
            {
                throw new InvalidOperationException($"Collection quantity {quantity} exceeds order quantity {screenOrder.Quantity}");
            }

            // For now, require full collection
            if (quantity != screenOrder.Quantity)
            {
                throw new InvalidOperationException($"Partial collections not supported. Must collect full quantity {screenOrder.Quantity}");
            }

            // Consume screens from inventory
            var screensConsumed = await _productService.ConsumeScreensAsync(quantity);
            if (!screensConsumed)
            {
                throw new InvalidOperationException($"Failed to consume {quantity} screens from inventory");
            }

            // Update order status to collected
            var statusUpdated = await _screenOrderService.UpdateStatusAsync(orderId, "collected");
            if (!statusUpdated)
            {
                throw new InvalidOperationException($"Failed to update order {orderId} status to collected");
            }

            _logger.LogInformation("Successfully prepared {Quantity} screens for collection from order {OrderId}", 
                quantity, orderId);

            return new CollectResponse
            {
                Success = true,
                OrderId = orderId,
                QuantityCollected = quantity,
                ItemType = "screens",
                Status = "collected",
                PreparedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling collect for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<(int ShippingID, int BankAccount)> RequestPickupAsync(int orderId, int quantity, string type, string fromCompany, string toCompany)
    {
        _logger.LogInformation("Requesting pickup for order {OrderId}: {Quantity} {Type} from {FromCompany} to {ToCompany}", 
            orderId, quantity, type, fromCompany, toCompany);

        try
        {
            var bulkLogisticsUrl = _configuration["ExternalServices:BulkLogistics:BaseUrl"];
            if (string.IsNullOrEmpty(bulkLogisticsUrl))
            {
                throw new InvalidOperationException("Bulk logistics URL not configured");
            }

            var requestData = new
            {
                orderId = orderId,
                quantity = quantity,
                type = type,
                from = fromCompany,
                to = toCompany,
                requestedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{bulkLogisticsUrl}/request-pickup", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to request pickup: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var pickupResponse = JsonSerializer.Deserialize<PickupResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (pickupResponse?.ShipmentId == null)
            {
                throw new InvalidOperationException("Invalid response from bulk logistics service");
            }

            _logger.LogInformation("Pickup requested successfully. Shipment ID: {ShipmentId}", pickupResponse.ShipmentId);
            
            return (pickupResponse.ShipmentId, pickupResponse.BankAccountNumber);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error requesting pickup for order {OrderId}", orderId);
            throw new InvalidOperationException("Failed to communicate with bulk logistics service", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting pickup for order {OrderId}", orderId);
            throw;
        }
    }
}