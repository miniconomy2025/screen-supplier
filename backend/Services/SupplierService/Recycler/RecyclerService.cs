using Microsoft.Extensions.Options;
using ScreenProducerAPI.Services.SupplierService.Recycler.Models;
using System.Text.Json;

namespace ScreenProducerAPI.Services.SupplierService.Recycler;

public class RecyclerService
{
    private readonly HttpClient httpClient;
    private readonly IOptions<SupplierServiceOptions> options;
    private readonly ILogger<RecyclerService> logger;
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RecyclerService(HttpClient httpClient, IOptions<SupplierServiceOptions> options, ILogger<RecyclerService> logger)
    {
        this.httpClient = httpClient;
        this.options = options;
        this.logger = logger;
    }

    public async Task<RecyclerMaterialsResponse> GetMaterialsAsync()
    {
        try
        {
            var baseUrl = options?.Value.RecyclerBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/materials");
            var response = await httpClient.GetAsync(uriBuilder.Uri);
            response.EnsureSuccessStatusCode();
            var materialsResponse = await response.Content.ReadFromJsonAsync<RecyclerMaterialsResponse>(jsonSerializerOptions);

            return materialsResponse ?? throw new Exception("Failed to retrieve recycler materials.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Error retrieving recycler materials", ex);
        }
    }

    public async Task<RecyclerOrderCreatedResponse> CreateOrderAsync(RecyclerOrderRequest request)
    {
        try
        {
            var baseUrl = options?.Value.RecyclerBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/orders");
            var response = await httpClient.PostAsJsonAsync(uriBuilder.Uri, request);

            response.EnsureSuccessStatusCode();

            var orderCreatedResponse = await response.Content.ReadFromJsonAsync<RecyclerOrderCreatedResponse>(jsonSerializerOptions);

            return orderCreatedResponse ?? throw new Exception("Failed to create recycler order.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Error creating recycler order", ex);
        }
    }

    public async Task<List<RecyclerOrderSummaryResponse>> GetOrdersAsync()
    {
        try
        {
            var baseUrl = options?.Value.RecyclerBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/orders");
            var response = await httpClient.GetAsync(uriBuilder.Uri);

            response.EnsureSuccessStatusCode();

            var ordersResponse = await response.Content.ReadFromJsonAsync<List<RecyclerOrderSummaryResponse>>(jsonSerializerOptions);

            return ordersResponse ?? throw new Exception("Failed to retrieve recycler orders.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Error retrieving recycler orders", ex);
        }
    }

    public async Task<RecyclerOrderDetailResponse> GetOrderByNumberAsync(string orderNumber)
    {
        try
        {
            var baseUrl = options?.Value.RecyclerBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/orders/{orderNumber}");
            var response = await httpClient.GetAsync(uriBuilder.Uri);

            response.EnsureSuccessStatusCode();

            var orderDetailResponse = await response.Content.ReadFromJsonAsync<RecyclerOrderDetailResponse>(jsonSerializerOptions);

            return orderDetailResponse ?? throw new Exception($"Failed to retrieve recycler order {orderNumber}.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error retrieving recycler order {orderNumber}", ex);
        }
    }
}
