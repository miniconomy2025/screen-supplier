using Microsoft.Extensions.Options;
using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Services.SupplierService;
using ScreenProducerAPI.Services.SupplierService.Hand.Models;
using System.Text.Json;

public class HandService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<SupplierServiceOptions> _options;
    private readonly ILogger<HandService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HandService(HttpClient httpClient, IOptions<SupplierServiceOptions> options, ILogger<HandService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<MachinesForSaleResponse> GetMachinesForSaleAsync()
    {
        try
        {
            var baseUrl = _options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/machines");
            var response = await _httpClient.GetAsync(uriBuilder.Uri);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HandServiceException($"Failed to retrieve machines: {response.StatusCode} - {errorContent}");
            }

            var machinesResponse = await response.Content.ReadFromJsonAsync<MachinesForSaleResponse>(_jsonOptions);

            if (machinesResponse == null)
                throw new HandServiceException("Invalid response format for machines data");

            return machinesResponse;
        }
        catch (HttpRequestException ex)
        {
            throw new HandServiceException("Hand service unavailable for machines retrieval", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new HandServiceException("Hand service timeout during machines retrieval", ex);
        }
    }

    public async Task<List<RawMaterialForSale>> GetRawMaterialsForSaleAsync()
    {
        try
        {
            var baseUrl = _options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/raw-materials");
            var response = await _httpClient.GetAsync(uriBuilder.Uri);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HandServiceException($"Failed to retrieve raw materials: {response.StatusCode} - {errorContent}");
            }

            var materialsResponse = await response.Content.ReadFromJsonAsync<List<RawMaterialForSale>>(_jsonOptions);

            if (materialsResponse == null)
                throw new HandServiceException("Invalid response format for raw materials data");

            return materialsResponse;
        }
        catch (HttpRequestException ex)
        {
            throw new HandServiceException("Hand service unavailable for raw materials retrieval", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new HandServiceException("Hand service timeout during raw materials retrieval", ex);
        }
    }

    public async Task<PurchaseMachineResponse> PurchaseMachineAsync(PurchaseMachineRequest request)
    {
        try
        {
            var baseUrl = _options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/purchase-machine");
            var response = await _httpClient.PostAsJsonAsync(uriBuilder.Uri, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Check for insufficient stock
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                    errorContent.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InsufficientStockException("machines", request.Quantity, 0);
                }

                throw new HandServiceException($"Machine purchase failed: {response.StatusCode} - {errorContent}");
            }

            var machineResponse = await response.Content.ReadFromJsonAsync<PurchaseMachineResponse>(_jsonOptions);

            if (machineResponse == null)
                throw new HandServiceException("Invalid response format for machine purchase");

            return machineResponse;
        }
        catch (InsufficientStockException)
        {
            throw; // Re-throw business exceptions
        }
        catch (HttpRequestException ex)
        {
            throw new HandServiceException("Hand service unavailable for machine purchase", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new HandServiceException("Hand service timeout during machine purchase", ex);
        }
    }

    public async Task<PurchaseRawMaterialResponse> PurchaseRawMaterialAsync(PurchaseRawMaterialRequest request)
    {
        try
        {
            var baseUrl = _options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/purchase-raw-material");
            var response = await _httpClient.PostAsJsonAsync(uriBuilder.Uri, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Check for insufficient stock
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                    errorContent.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InsufficientStockException(request.MaterialName, (int)request.WeightQuantity, 0);
                }

                throw new HandServiceException($"Raw material purchase failed: {response.StatusCode} - {errorContent}");
            }

            var materialResponse = await response.Content.ReadFromJsonAsync<PurchaseRawMaterialResponse>(_jsonOptions);

            if (materialResponse == null)
                throw new HandServiceException("Invalid response format for raw material purchase");

            return materialResponse;
        }
        catch (InsufficientStockException)
        {
            throw; // Re-throw business exceptions
        }
        catch (HttpRequestException ex)
        {
            throw new HandServiceException("Hand service unavailable for raw material purchase", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new HandServiceException("Hand service timeout during raw material purchase", ex);
        }
    }

    public async Task<SimulationTimeResponse> GetCurrentSimulationTimeAsync()
    {
        try
        {
            var baseUrl = _options?.Value.HandBaseUrl;
            var uriBuilder = new UriBuilder($"{baseUrl}/simulation/current-simulation-time");
            var response = await _httpClient.GetAsync(uriBuilder.Uri);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HandServiceException($"Failed to retrieve simulation time: {response.StatusCode} - {errorContent}");
            }

            var timeResponse = await response.Content.ReadFromJsonAsync<SimulationTimeResponse>(_jsonOptions);

            if (timeResponse == null)
                throw new HandServiceException("Invalid response format for simulation time");

            return timeResponse;
        }
        catch (HttpRequestException ex)
        {
            throw new HandServiceException("Hand service unavailable for simulation time retrieval", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new HandServiceException("Hand service timeout during simulation time retrieval", ex);
        }
    }

    public async Task<bool> TryInitializeEquipmentParametersAsync(EquipmentService equipmentService)
    {
        try
        {
            _logger.LogInformation("Fetching equipment parameters from Hand service...");

            var machinesResponse = await GetMachinesForSaleAsync();
            var screenMachine = machinesResponse.Machines.FirstOrDefault(m => m.MachineName == "screen_machine");

            if (screenMachine == null)
            {
                _logger.LogWarning("Screen machine not found in Hand service response.");
                return false;
            }

            var (sandKg, copperKg) = (screenMachine.InputRatio.Sand, screenMachine.InputRatio.Copper);
            var outputScreensPerDay = screenMachine.ProductionRate;

            _logger.LogInformation("Found screen machine - Sand: {SandKg}kg, Copper: {CopperKg}kg, Output: {OutputScreens} screens/day",
                sandKg, copperKg, outputScreensPerDay);

            return await equipmentService.InitializeEquipmentParametersAsync(sandKg, copperKg, outputScreensPerDay, screenMachine.Weight);
        }
        catch (HandServiceException ex)
        {
            _logger.LogWarning("Failed to fetch equipment parameters from Hand service: {Message}.", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error initializing equipment parameters.");
            return false;
        }
    }
}