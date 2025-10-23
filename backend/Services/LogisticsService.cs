using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Models.Requests;
using ScreenProducerAPI.Models.Responses;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Util;
using System.Text;
using System.Text.Json;

namespace ScreenProducerAPI.Services;

public class LogisticsService : ILogisticsService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IScreenOrderService _screenOrderService;
    private readonly IMaterialService _materialService;
    private readonly IEquipmentService _equipmentService;
    private readonly IProductService _productService;
    private readonly ScreenContext _context;
    private readonly ISimulationTimeProvider _simulationTimeProvider;

    public LogisticsService(
        IConfiguration configuration,
        HttpClient httpClient,
        IPurchaseOrderService purchaseOrderService,
        IScreenOrderService screenOrderService,
        IMaterialService materialService,
        IEquipmentService equipmentService,
        IProductService productService,
        ISimulationTimeProvider simulationTimeProvider,
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

    public async Task<LogisticsResponse> HandleDropoffAsync(int quantityIn, int shipmentId)
    {
        var purchaseOrder = await _purchaseOrderService.FindPurchaseOrderByShipmentIdAsync(shipmentId);
        if (purchaseOrder == null)
            throw new OrderNotFoundException(shipmentId);

        int quantity = purchaseOrder.EquipmentOrder == true ? 1 : quantityIn;

        var waitingDeliveryStatus = await _context.OrderStatuses
            .FirstOrDefaultAsync(os => os.Status == Status.WaitingForDelivery);

        if (purchaseOrder.OrderStatusId != waitingDeliveryStatus?.Id)
            throw new InvalidOrderStateException(shipmentId, purchaseOrder.OrderStatus?.Status ?? "unknown", Status.WaitingForDelivery);

        var remainingQuantity = purchaseOrder.Quantity - purchaseOrder.QuantityDelivered;
        if (quantity > remainingQuantity)
            throw new InvalidRequestException($"Delivery quantity {quantity} exceeds remaining order quantity {remainingQuantity}");

        string itemType = "";
        bool processed = false;

        if (purchaseOrder.EquipmentOrder == true)
        {
            processed = await _equipmentService.AddEquipmentAsync(purchaseOrder.Id);
            itemType = "equipment";
        }
        else if (purchaseOrder.RawMaterialId.HasValue && purchaseOrder.RawMaterial != null)
        {
            var materialName = purchaseOrder.RawMaterial.Name;
            processed = await _materialService.AddMaterialAsync(materialName, quantity);
            itemType = materialName;
        }
        else
        {
            throw new SystemConfigurationException($"Purchase order {purchaseOrder.Id} has invalid configuration");
        }

        if (!processed)
            throw new InvalidOperationException($"Failed to process {itemType} delivery");

        await _purchaseOrderService.UpdateDeliveryQuantityAsync(purchaseOrder.Id, quantity);

        return new LogisticsResponse
        {
            Success = true,
            Id = shipmentId,
            OrderId = purchaseOrder.OrderID,
            Quantity = quantity,
            ItemType = itemType,
            Message = $"Successfully received {quantity} units of {itemType}",
            ProcessedAt = _simulationTimeProvider.Now,
        };
    }

    public async Task<LogisticsResponse?> HandleCollectAsync(int quantity, int orderId)
    {

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

            var remainingToCollectQuantity = screenOrder.Quantity - screenOrder.QuantityCollected;
            if (quantity > remainingToCollectQuantity)
            {
                throw new InvalidOperationException($"Collection quantity {quantity} exceeds remaining order quantity {remainingToCollectQuantity}");
            }

            // Consume screens from inventory for those collected
            var screensConsumed = await _productService.ConsumeScreensAsync(quantity);
            if (!screensConsumed)
            {
                throw new InvalidOperationException($"Failed to consume {quantity} screens from inventory");
            }

            await _screenOrderService.UpdateQuantityCollectedAsync(screenOrder.Id, quantity);

            return new LogisticsResponse
            {
                Success = true,
                OrderId = orderId,
                Quantity = quantity,
                ItemType = "screens",
                Message = Status.Collected,
                ProcessedAt = _simulationTimeProvider.Now
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
                throw new SystemConfigurationException("Bulk logistics URL not configured");
            }

            var requestData = new PickupRequestBody
            {
                OriginCompany = originCompanyId,
                DestinationCompany = destinationCompanyId,
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
                throw new LogisticsServiceException($"Pickup request failed: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var pickupResponse = JsonSerializer.Deserialize<PickupRequestResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (pickupResponse?.PickupRequestId == null)
            {
                throw new LogisticsServiceException("Invalid response from bulk logistics service - missing pickup request ID");
            }

            return (pickupResponse.PickupRequestId.ToString(), pickupResponse.AccountNumber, (int)Math.Ceiling(decimal.Parse(pickupResponse.Cost)));
        }
        catch (HttpRequestException ex)
        {
            throw new LogisticsServiceException("Bulk logistics service unavailable", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new LogisticsServiceException("Bulk logistics service timeout", ex);
        }
        catch (JsonException ex)
        {
            throw new LogisticsServiceException("Invalid response format from bulk logistics service", ex);
        }
    }

    public static List<PickupRequestItem> CreatePickupItems(string itemType, int quantity, bool isEquipment = false)
    {
        var measurementType = isEquipment ? "UNIT" : "KG";
        var itemName = itemType.ToLower() switch
        {
            "equipment" => "screen_machine",
            "sand" => "sand",
            "copper" => "copper",
            "screens" => "screens",
            _ => itemType.ToLower()
        };

        if (itemName == "screen_machine")
        {
            quantity = 1;
        }

        return new List<PickupRequestItem>
        {
            new PickupRequestItem
            {
                ItemName = itemName,
                Quantity = quantity == 0 ? 1: quantity,
                MeasurementType = measurementType
            }
        };
    }
}