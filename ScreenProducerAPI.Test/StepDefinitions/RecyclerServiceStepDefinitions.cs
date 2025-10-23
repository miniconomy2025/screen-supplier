using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Reqnroll;
using ScreenProducerAPI.Exceptions;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.SupplierService;
using ScreenProducerAPI.Services.SupplierService.Recycler;
using ScreenProducerAPI.Services.SupplierService.Recycler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
public sealed class RecyclerServiceStepDefinitions
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private IRecyclerService _recyclerService = null!;
    private Mock<ILogger<RecyclerService>> _mockLogger = null!;
    private Mock<IOptions<SupplierServiceOptions>> _mockOptions = null!;

    private List<RecyclerMaterial>? _materialsResult;
    private RecyclerOrderCreatedResponse? _orderCreatedResult;
    private List<RecyclerOrderSummaryResponse>? _ordersResult;
    private RecyclerOrderDetailResponse? _orderDetailResult;
    
    private RecyclerOrderRequest? _orderRequest;
    private Exception? _caughtException;
    
    private string _serviceState = "available";
    private List<RecyclerOrderSummaryResponse> _existingOrders = new();
    private Dictionary<string, RecyclerOrderDetailResponse> _existingOrderDetails = new();

    [BeforeScenario]
    public void SetupContext()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockLogger = new Mock<ILogger<RecyclerService>>();
        _mockOptions = new Mock<IOptions<SupplierServiceOptions>>();

        _mockOptions.Setup(x => x.Value).Returns(new SupplierServiceOptions
        {
            RecyclerBaseUrl = "http://test-recycler.com"
        });

        _recyclerService = new RecyclerService(_httpClient, _mockOptions.Object, _mockLogger.Object);

        // Reset state
        _materialsResult = null;
        _orderCreatedResult = null;
        _ordersResult = null;
        _orderDetailResult = null;
        _orderRequest = null;
        _caughtException = null;
        _serviceState = "available";
        _existingOrders = new();
        _existingOrderDetails = new();
    }

    #region Given Steps

    [Given(@"the recycler service is available")]
    public void GivenTheRecyclerServiceIsAvailable()
    {
        _serviceState = "available";
    }

    [Given(@"the recycler service is unavailable")]
    public void GivenTheRecyclerServiceIsUnavailable()
    {
        _serviceState = "unavailable";
    }

    [Given(@"the recycler service times out")]
    public void GivenTheRecyclerServiceTimesOut()
    {
        _serviceState = "timeout";
    }

    [Given(@"the recycler service returns invalid materials response")]
    public void GivenTheRecyclerServiceReturnsInvalidMaterialsResponse()
    {
        _serviceState = "invalid_materials_response";
    }

    [Given(@"the recycler service returns invalid orders response")]
    public void GivenTheRecyclerServiceReturnsInvalidOrdersResponse()
    {
        _serviceState = "invalid_orders_response";
    }

    [Given(@"the recycler service returns invalid order detail response")]
    public void GivenTheRecyclerServiceReturnsInvalidOrderDetailResponse()
    {
        _serviceState = "invalid_order_detail_response";
    }

    [Given(@"the recycler service returns invalid order creation response")]
    public void GivenTheRecyclerServiceReturnsInvalidOrderCreationResponse()
    {
        _serviceState = "invalid_order_creation_response";
    }

    [Given(@"the recycler service returns insufficient stock error")]
    public void GivenTheRecyclerServiceReturnsInsufficientStockError()
    {
        _serviceState = "insufficient_stock";
    }

    [Given(@"I have a valid recycler order request with company ""(.*)""")]
    public void GivenIHaveAValidRecyclerOrderRequestWithCompany(string companyName)
    {
        _orderRequest = new RecyclerOrderRequest
        {
            CompanyName = companyName,
            OrderItems = new List<RecyclerOrderItem>()
        };
    }

    [Given(@"the order has item ""(.*)"" with quantity (.*)")]
    public void GivenTheOrderHasItemWithQuantity(string materialName, int quantity)
    {
        if (_orderRequest == null)
        {
            _orderRequest = new RecyclerOrderRequest
            {
                CompanyName = "TestCompany",
                OrderItems = new List<RecyclerOrderItem>()
            };
        }

        _orderRequest.OrderItems.Add(new RecyclerOrderItem
        {
            RawMaterialName = materialName,
            QuantityInKg = quantity
        });
    }

    [Given(@"there are existing recycler orders")]
    public void GivenThereAreExistingRecyclerOrders()
    {
        _existingOrders = new List<RecyclerOrderSummaryResponse>
        {
            new RecyclerOrderSummaryResponse
            {
                OrderNumber = "RECYC-00000001",
                SupplierName = "Mock Recycler",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            },
            new RecyclerOrderSummaryResponse
            {
                OrderNumber = "RECYC-00000002",
                SupplierName = "Mock Recycler",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
    }

    [Given(@"there are no existing recycler orders")]
    public void GivenThereAreNoExistingRecyclerOrders()
    {
        _existingOrders = new List<RecyclerOrderSummaryResponse>();
    }

    [Given(@"there is an existing recycler order with number ""(.*)""")]
    public void GivenThereIsAnExistingRecyclerOrderWithNumber(string orderNumber)
    {
        _existingOrderDetails[orderNumber] = new RecyclerOrderDetailResponse
        {
            OrderNumber = orderNumber,
            SupplierName = "Mock Recycler",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            Items = new List<RecyclerOrderDetailItem>
            {
                new RecyclerOrderDetailItem
                {
                    Material = "Sand",
                    Quantity = 100,
                    Price = 800
                }
            }
        };
    }

    [Given(@"there is no recycler order with number ""(.*)""")]
    public void GivenThereIsNoRecyclerOrderWithNumber(string orderNumber)
    {
        // Ensure the order doesn't exist
        if (_existingOrderDetails.ContainsKey(orderNumber))
        {
            _existingOrderDetails.Remove(orderNumber);
        }
    }

    #endregion

    #region When Steps

    [When(@"I get materials from the recycler service")]
    public async Task WhenIGetMaterialsFromTheRecyclerService()
    {
        SetupHttpResponse("materials", null);
        
        try
        {
            _materialsResult = await _recyclerService.GetMaterialsAsync();
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }
    }

    [When(@"I create a recycler order")]
    public async Task WhenICreateARecyclerOrder()
    {
        SetupHttpResponse("orders", _orderRequest);
        
        try
        {
            _orderCreatedResult = await _recyclerService.CreateOrderAsync(_orderRequest!);
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }
    }

    [When(@"I get all recycler orders")]
    public async Task WhenIGetAllRecyclerOrders()
    {
        SetupHttpResponse("orders", null);
        
        try
        {
            _ordersResult = await _recyclerService.GetOrdersAsync();
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }
    }

    [When(@"I get recycler order by number ""(.*)""")]
    public async Task WhenIGetRecyclerOrderByNumber(string orderNumber)
    {
        SetupHttpResponse($"orders/{orderNumber}", null);
        
        try
        {
            _orderDetailResult = await _recyclerService.GetOrderByNumberAsync(orderNumber);
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }
    }

    #endregion

    #region Then Steps

    [Then(@"the materials operation should succeed")]
    public void ThenTheMaterialsOperationShouldSucceed()
    {
        _caughtException.Should().BeNull();
        _materialsResult.Should().NotBeNull();
    }

    [Then(@"the materials list should not be empty")]
    public void ThenTheMaterialsListShouldNotBeEmpty()
    {
        _materialsResult.Should().NotBeNull();
        _materialsResult.Should().NotBeEmpty();
    }

    [Then(@"all materials should have valid properties")]
    public void ThenAllMaterialsShouldHaveValidProperties()
    {
        _materialsResult.Should().NotBeNull();
        foreach (var material in _materialsResult!)
        {
            material.Id.Should().BeGreaterThan(0);
            material.Name.Should().NotBeNullOrEmpty();
            material.AvailableQuantityInKg.Should().BeGreaterThan(0);
            material.PricePerKg.Should().BeGreaterThan(0);
        }
    }

    [Then(@"the materials operation should throw RecyclerServiceException")]
    public void ThenTheMaterialsOperationShouldThrowRecyclerServiceException()
    {
        _caughtException.Should().NotBeNull();
        _caughtException.Should().BeOfType<RecyclerServiceException>();
    }

    [Then(@"the exception message should contain ""(.*)""")]
    public void ThenTheExceptionMessageShouldContain(string expectedMessage)
    {
        _caughtException.Should().NotBeNull();
        _caughtException!.Message.Should().Contain(expectedMessage);
    }

    [Then(@"the create order operation should succeed")]
    public void ThenTheCreateOrderOperationShouldSucceed()
    {
        _caughtException.Should().BeNull();
        _orderCreatedResult.Should().NotBeNull();
    }

    [Then(@"the order response should have an order ID")]
    public void ThenTheOrderResponseShouldHaveAnOrderID()
    {
        _orderCreatedResult.Should().NotBeNull();
        _orderCreatedResult!.data.Should().NotBeNull();
        _orderCreatedResult.data.OrderId.Should().BeGreaterThan(0);
    }

    [Then(@"the order response should have an account number")]
    public void ThenTheOrderResponseShouldHaveAnAccountNumber()
    {
        _orderCreatedResult.Should().NotBeNull();
        _orderCreatedResult!.data.Should().NotBeNull();
        _orderCreatedResult.data.AccountNumber.Should().NotBeNullOrEmpty();
    }

    [Then(@"the create order operation should throw InsufficientStockException")]
    public void ThenTheCreateOrderOperationShouldThrowInsufficientStockException()
    {
        _caughtException.Should().NotBeNull();
        _caughtException.Should().BeOfType<InsufficientStockException>();
    }

    [Then(@"the create order operation should throw RecyclerServiceException")]
    public void ThenTheCreateOrderOperationShouldThrowRecyclerServiceException()
    {
        _caughtException.Should().NotBeNull();
        _caughtException.Should().BeOfType<RecyclerServiceException>();
    }

    [Then(@"the get orders operation should succeed")]
    public void ThenTheGetOrdersOperationShouldSucceed()
    {
        _caughtException.Should().BeNull();
        _ordersResult.Should().NotBeNull();
    }

    [Then(@"the orders list should not be empty")]
    public void ThenTheOrdersListShouldNotBeEmpty()
    {
        _ordersResult.Should().NotBeNull();
        _ordersResult.Should().NotBeEmpty();
    }

    [Then(@"the orders list should be empty")]
    public void ThenTheOrdersListShouldBeEmpty()
    {
        _ordersResult.Should().NotBeNull();
        _ordersResult.Should().BeEmpty();
    }

    [Then(@"all orders should have valid properties")]
    public void ThenAllOrdersShouldHaveValidProperties()
    {
        _ordersResult.Should().NotBeNull();
        foreach (var order in _ordersResult!)
        {
            order.OrderNumber.Should().NotBeNullOrEmpty();
            order.SupplierName.Should().NotBeNullOrEmpty();
            order.Status.Should().NotBeNullOrEmpty();
            order.CreatedAt.Should().NotBe(default(DateTime));
        }
    }

    [Then(@"the get orders operation should throw RecyclerServiceException")]
    public void ThenTheGetOrdersOperationShouldThrowRecyclerServiceException()
    {
        _caughtException.Should().NotBeNull();
        _caughtException.Should().BeOfType<RecyclerServiceException>();
    }

    [Then(@"the get order by number operation should succeed")]
    public void ThenTheGetOrderByNumberOperationShouldSucceed()
    {
        _caughtException.Should().BeNull();
        _orderDetailResult.Should().NotBeNull();
    }

    [Then(@"the order detail should have order number ""(.*)""")]
    public void ThenTheOrderDetailShouldHaveOrderNumber(string expectedOrderNumber)
    {
        _orderDetailResult.Should().NotBeNull();
        _orderDetailResult!.OrderNumber.Should().Be(expectedOrderNumber);
    }

    [Then(@"the order detail should have valid items")]
    public void ThenTheOrderDetailShouldHaveValidItems()
    {
        _orderDetailResult.Should().NotBeNull();
        _orderDetailResult!.Items.Should().NotBeNull();
        _orderDetailResult.Items.Should().NotBeEmpty();
        
        foreach (var item in _orderDetailResult.Items)
        {
            item.Material.Should().NotBeNullOrEmpty();
            item.Quantity.Should().BeGreaterThan(0);
            item.Price.Should().BeGreaterThan(0);
        }
    }

    [Then(@"the get order by number operation should throw DataNotFoundException")]
    public void ThenTheGetOrderByNumberOperationShouldThrowDataNotFoundException()
    {
        _caughtException.Should().NotBeNull();
        _caughtException.Should().BeOfType<DataNotFoundException>();
    }

    [Then(@"the get order by number operation should throw RecyclerServiceException")]
    public void ThenTheGetOrderByNumberOperationShouldThrowRecyclerServiceException()
    {
        _caughtException.Should().NotBeNull();
        _caughtException.Should().BeOfType<RecyclerServiceException>();
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(string endpoint, RecyclerOrderRequest? request)
    {
        var requestUri = $"http://test-recycler.com/{endpoint}";

        switch (_serviceState)
        {
            case "unavailable":
                _mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .ThrowsAsync(new HttpRequestException("Network error"));
                break;

            case "timeout":
                _mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .ThrowsAsync(new TaskCanceledException("Timeout"));
                break;

            case "insufficient_stock":
                _mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        Content = new StringContent("Insufficient stock available")
                    });
                break;

            case "invalid_materials_response":
                _mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/materials")),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("null")
                    });
                break;

            case "invalid_orders_response":
                _mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().EndsWith("/orders") && req.Method == HttpMethod.Get),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("null")
                    });
                break;

            case "invalid_order_detail_response":
                _mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/orders/") && req.Method == HttpMethod.Get),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("null")
                    });
                break;

            case "invalid_order_creation_response":
                _mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("null")
                    });
                break;

            case "available":
            default:
                SetupSuccessfulResponse(endpoint, request);
                break;
        }
    }

    private void SetupSuccessfulResponse(string endpoint, RecyclerOrderRequest? request)
    {
        if (endpoint == "materials")
        {
            var materials = new List<RecyclerMaterial>
            {
                new RecyclerMaterial
                {
                    Id = 1,
                    Name = "Sand",
                    AvailableQuantityInKg = 10000,
                    PricePerKg = 8
                },
                new RecyclerMaterial
                {
                    Id = 2,
                    Name = "Copper",
                    AvailableQuantityInKg = 5000,
                    PricePerKg = 40
                }
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/materials")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(materials))
                });
        }
        else if (endpoint == "orders" && request != null)
        {
            var orderResponse = new RecyclerOrderCreatedResponse
            {
                data = new Data
                {
                    OrderId = 123,
                    AccountNumber = "MOCK-RECYCLER-ACC",
                    OrderItems = request.OrderItems.Select(item => new OrderItem
                    {
                        QuantityInKg = item.QuantityInKg,
                        PricePerKg = item.RawMaterialName.ToLower() == "sand" ? 8 : 40
                    }).ToList()
                }
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(orderResponse))
                });
        }
        else if (endpoint == "orders")
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().EndsWith("/orders") && req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(_existingOrders))
                });
        }
        else if (endpoint.StartsWith("orders/"))
        {
            var orderNumber = endpoint.Replace("orders/", "");
            
            if (_existingOrderDetails.ContainsKey(orderNumber))
            {
                _mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains($"/orders/{orderNumber}")),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(_existingOrderDetails[orderNumber]))
                    });
            }
            else
            {
                _mockHttpMessageHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains($"/orders/{orderNumber}")),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        Content = new StringContent($"Order {orderNumber} not found")
                    });
            }
        }
    }

    #endregion

    [AfterScenario]
    public void AfterScenario()
    {
        _httpClient?.Dispose();
    }
}
