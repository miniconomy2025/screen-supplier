using Microsoft.Extensions.Options;
using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Services.SupplierService;
using ScreenProducerAPI.Services.SupplierService.Recycler.Models;
using System.Text.Json;

public class RecyclerService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<SupplierServiceOptions> _options;
    private readonly ILogger<RecyclerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RecyclerService(HttpClient httpClient, IOptions<SupplierServiceOptions> options, ILogger<RecyclerService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<List<RecyclerMaterial>> GetMaterialsAsync()
    {
        try
        {
            var baseUrl = _options?.Value.RecyclerBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/materials");
            var response = await _httpClient.GetAsync(uriBuilder.Uri);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new RecyclerServiceException($"Failed to retrieve materials: {response.StatusCode} - {errorContent}");
            }

            var materialsResponse = await response.Content.ReadFromJsonAsync<List<RecyclerMaterial>>(_jsonOptions);

            if (materialsResponse == null)
                throw new RecyclerServiceException("Invalid response format for materials data");

            return materialsResponse;
        }
        catch (HttpRequestException ex)
        {
            throw new RecyclerServiceException("Recycler service unavailable for materials retrieval", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new RecyclerServiceException("Recycler service timeout during materials retrieval", ex);
        }
    }

    public async Task<RecyclerOrderCreatedResponse> CreateOrderAsync(RecyclerOrderRequest request)
    {
        try
        {
            var baseUrl = _options?.Value.RecyclerBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/orders");
            var response = await _httpClient.PostAsJsonAsync(uriBuilder.Uri, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Check for insufficient stock
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                    errorContent.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InsufficientStockException("recycled materials", 1, 0);
                }

                throw new RecyclerServiceException($"Order creation failed: {response.StatusCode} - {errorContent}");
            }

            var orderCreatedResponse = await response.Content.ReadFromJsonAsync<RecyclerOrderCreatedResponse>(_jsonOptions);

            if (orderCreatedResponse == null)
                throw new RecyclerServiceException("Invalid response format for order creation");

            return orderCreatedResponse;
        }
        catch (InsufficientStockException)
        {
            throw; // Re-throw business exceptions
        }
        catch (HttpRequestException ex)
        {
            throw new RecyclerServiceException("Recycler service unavailable for order creation", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new RecyclerServiceException("Recycler service timeout during order creation", ex);
        }
    }

    public async Task<List<RecyclerOrderSummaryResponse>> GetOrdersAsync()
    {
        try
        {
            var baseUrl = _options?.Value.RecyclerBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/orders");
            var response = await _httpClient.GetAsync(uriBuilder.Uri);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new RecyclerServiceException($"Failed to retrieve orders: {response.StatusCode} - {errorContent}");
            }

            var ordersResponse = await response.Content.ReadFromJsonAsync<List<RecyclerOrderSummaryResponse>>(_jsonOptions);

            if (ordersResponse == null)
                throw new RecyclerServiceException("Invalid response format for orders data");

            return ordersResponse;
        }
        catch (HttpRequestException ex)
        {
            throw new RecyclerServiceException("Recycler service unavailable for orders retrieval", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new RecyclerServiceException("Recycler service timeout during orders retrieval", ex);
        }
    }

    public async Task<RecyclerOrderDetailResponse> GetOrderByNumberAsync(string orderNumber)
    {
        try
        {
            var baseUrl = _options?.Value.RecyclerBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/orders/{orderNumber}");
            var response = await _httpClient.GetAsync(uriBuilder.Uri);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new DataNotFoundException($"Recycler order {orderNumber}");

                var errorContent = await response.Content.ReadAsStringAsync();
                throw new RecyclerServiceException($"Failed to retrieve order {orderNumber}: {response.StatusCode} - {errorContent}");
            }

            var orderDetailResponse = await response.Content.ReadFromJsonAsync<RecyclerOrderDetailResponse>(_jsonOptions);

            if (orderDetailResponse == null)
                throw new RecyclerServiceException($"Invalid response format for order {orderNumber}");

            return orderDetailResponse;
        }
        catch (DataNotFoundException)
        {
            throw; // Re-throw business exceptions
        }
        catch (HttpRequestException ex)
        {
            throw new RecyclerServiceException($"Recycler service unavailable for order {orderNumber} retrieval", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new RecyclerServiceException($"Recycler service timeout during order {orderNumber} retrieval", ex);
        }
    }
}