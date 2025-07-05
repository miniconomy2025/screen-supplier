using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Util;
using System.Text;
using System.Text.Json;

namespace ScreenProducerAPI.Services;

public class LogisticsService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly ScreenOrderService _screenOrderService;
    private readonly MaterialService _materialService;
    private readonly EquipmentService _equipmentService;
    private readonly ProductService _productService;
    private readonly ScreenContext _context;
    private readonly SimulationTimeProvider _simulationTimeProvider;

    public LogisticsService(
        IConfiguration configuration,
        HttpClient httpClient,
        PurchaseOrderService purchaseOrderService,
        ScreenOrderService screenOrderService,
        MaterialService materialService,
        EquipmentService equipmentService,
        ProductService productService,
        SimulationTimeProvider simulationTimeProvider,
        ScreenContext context)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _purchaseOrderService = purchaseOrderService;
        _screenOrderService = screenOrderService;
        _materialService = materialService;
        _equipmentService = equipmentService;
        _productService = productService;
        _simulationTimeProvider = simulationTimeProvider;   
        _context = context;
    }

    public async Task<DropoffResponse> HandleDropoffAsync(DropoffRequest request)
    {
        int shipmentId = request.Id;
        int quantity = request.Quantity;

        try
        {
            // Find purchase order by shipment ID
            var purchaseOrder = await _purchaseOrderService.FindPurchaseOrderByShipmentIdAsync(shipmentId);
            if (purchaseOrder == null)
            {
                throw new InvalidOperationException($"Purchase order with shipment ID {shipmentId} not found");
            }

            // Validate in waiting for delivery state
            var waitingDeliveryStatus = await _context.OrderStatuses.FirstOrDefaultAsync(os => os.Status == Status.WaitingForDelivery);
            if (purchaseOrder.OrderStatusId != waitingDeliveryStatus.Id)
            {
                throw new InvalidOperationException($"Purchase order with shipment ID {shipmentId} not waiting for delivery.");
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
            }
            // Handle material delivery
            else if (purchaseOrder.RawMaterialId.HasValue && purchaseOrder.RawMaterial != null)
            {
                var materialName = purchaseOrder.RawMaterial.Name;
                processed = await _materialService.AddMaterialAsync(materialName, quantity);
                itemType = materialName;
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
                ProcessedAt = _simulationTimeProvider.Now,
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<CollectResponse?> HandleCollectAsync(CollectRequest request)
    {
        int orderId = request.Id;
        int quantity = request.Quantity;

        try
        {
            // Find screen order by ID
            var screenOrder = await _screenOrderService.FindScreenOrderByIdAsync(orderId);
            if (screenOrder == null)
            {
                return null;
            }

            // Check if order is ready for collection
            if (screenOrder.OrderStatus?.Status != Status.WaitingForCollection)
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
            var statusUpdated = await _screenOrderService.UpdateStatusAsync(orderId, Status.Collected);
            if (!statusUpdated)
            {
                throw new InvalidOperationException($"Failed to update order {orderId} status to collected");
            }

            return new CollectResponse
            {
                Success = true,
                OrderId = orderId,
                QuantityCollected = quantity,
                ItemType = "screens",
                Status = Status.Collected,
                PreparedAt = _simulationTimeProvider.Now
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<(string PickupRequestId, string BankAccountNumber, int Price)> RequestPickupAsync(
        string originCompanyId,
        string destinationCompanyId,
        string originalExternalOrderId,
        List<PickupRequestItem> items)
    {
        try
        {
            var bulkLogisticsUrl = _configuration["ExternalServices:BulkLogistics:BaseUrl"];
            if (string.IsNullOrEmpty(bulkLogisticsUrl))
            {
                throw new InvalidOperationException("Bulk logistics URL not configured");
            }

            var requestData = new PickupRequestBody
            {
                OriginCompanyId = originCompanyId,
                DestinationCompanyId = destinationCompanyId,
                OriginalExternalOrderId = originalExternalOrderId,
                Items = items
            };

            var json = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{bulkLogisticsUrl}/pickup-request", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to request pickup: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var pickupResponse = JsonSerializer.Deserialize<PickupRequestResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (pickupResponse?.PickupRequestId == null)
            {
                throw new InvalidOperationException("Invalid response from bulk logistics service");
            }

            return (pickupResponse.PickupRequestId, pickupResponse.BankAccountNumber, pickupResponse.Price);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Failed to communicate with bulk logistics service", ex);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public static List<PickupRequestItem> CreatePickupItems(string itemType, double quantity, bool isEquipment = false)
    {
        var measurementType = isEquipment ? "UNIT" : "KG";
        var itemName = itemType.ToLower() switch
        {
            "equipment" => "machine",
            "sand" => "sand",
            "copper" => "copper",
            "screens" => "screens",
            _ => itemType.ToLower()
        };

        return new List<PickupRequestItem>
        {
            new PickupRequestItem
            {
                Name = itemName,
                Quantity = quantity,
                MeasurementType = measurementType
            }
        };
    }
}