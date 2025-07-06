using Microsoft.Extensions.Options;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;
using System.Text.Json;

namespace ScreenProducerAPI.Services.SupplierService.Hand;

public class HandService(HttpClient httpClient, IOptions<SupplierServiceOptions> options,
    ILogger<HandService> _logger)
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<string> GetSimulationUnixEpochStartTimeAsync()
    {
        try
        {
            var baseUrl = options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/unix-epoch-start-time");
            var response = await httpClient.GetAsync(uriBuilder.Uri);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve simulation unix epoch start time");
            throw new InvalidOperationException("Failed to retrieve simulation unix epoch start time", ex);
        }
    }

    public async Task<SimulationTimeResponse> GetCurrentSimulationTimeAsync()
    {
        try
        {
            var baseUrl = options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/current-simulation-time");
            var response = await httpClient.GetAsync(uriBuilder.Uri);

            response.EnsureSuccessStatusCode();

            var timeResponse = await response.Content.ReadFromJsonAsync<SimulationTimeResponse>(jsonSerializerOptions)
                ?? throw new InvalidOperationException("Unexpected content type received from Hand service");

            return timeResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve current simulation time");
            throw new InvalidOperationException("Failed to retrieve current simulation time", ex);
        }
    }

    public async Task<PurchaseMachineResponse> PurchaseMachineAsync(PurchaseMachineRequest request)
    {
        try
        {
            var baseUrl = options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/purchase-machine");
            var response = await httpClient.PostAsJsonAsync(uriBuilder.Uri, request);
            response.EnsureSuccessStatusCode();

            var machineResponse = await response.Content.ReadFromJsonAsync<PurchaseMachineResponse>(jsonSerializerOptions)
                ?? throw new InvalidOperationException("Unexpected content type received from Hand service");

            return machineResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to purchase machine");
            throw new InvalidOperationException("Failed to purchase machine", ex);
        }
    }

    public async Task<PurchaseRawMaterialResponse> PurchaseRawMaterialAsync(PurchaseRawMaterialRequest request)
    {
        try
        {
            var baseUrl = options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/purchase-raw-material");
            var response = await httpClient.PostAsJsonAsync(uriBuilder.Uri, request);
            response.EnsureSuccessStatusCode();

            var materialResponse = await response.Content.ReadFromJsonAsync<PurchaseRawMaterialResponse>(jsonSerializerOptions)
                ?? throw new InvalidOperationException("Unexpected content type received from Hand service");

            return materialResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to purchase raw material");
            throw new InvalidOperationException("Failed to purchase raw material", ex);
        }
    }

    public async Task<MachinesForSaleResponse> GetMachinesForSaleAsync()
    {
        try
        {
            var baseUrl = options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/machines");
            var response = await httpClient.GetAsync(uriBuilder.Uri);
            response.EnsureSuccessStatusCode();

            var machinesResponse = await response.Content.ReadFromJsonAsync<MachinesForSaleResponse>(jsonSerializerOptions)
                ?? throw new InvalidOperationException("Unexpected content type received from Hand service");

            return machinesResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve machines for sale");
            throw new InvalidOperationException("Failed to retrieve machines for sale", ex);
        }
    }

    public async Task<List<RawMaterialForSale>> GetRawMaterialsForSaleAsync()
    {
        try
        {
            var baseUrl = options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/raw-materials");
            var response = await httpClient.GetAsync(uriBuilder.Uri);
            response.EnsureSuccessStatusCode();

            var materialsResponse = await response.Content.ReadFromJsonAsync<List<RawMaterialForSale>>(jsonSerializerOptions)
                ?? throw new InvalidOperationException("Unexpected content type received from Hand service");

            return materialsResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve raw materials for sale");
            throw new InvalidOperationException("Failed to retrieve raw materials for sale", ex);
        }
    }
}
