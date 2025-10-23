using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using ScreenProducerAPI.Util;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
public sealed class ProductServiceStepDefinitions
{
    private ScreenContext _context = null!;
    private Mock<IMaterialService> _mockMaterialService = null!;
    private IProductService _productService = null!;

    private bool _boolResult;
    private int _intResult;
    private (int totalProduced, int reserved, int available) _stockSummary;

    [BeforeScenario]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ScreenContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ScreenContext(options);

        // Mock the interface instead of the concrete class
        _mockMaterialService = new Mock<IMaterialService>();

        // Inject the mock into your ProductService
        _productService = new ProductService(_context, _mockMaterialService.Object);
    }

    [Given(@"there are no products")]
    public void GivenThereAreNoProducts()
    {
        _context.Products.RemoveRange(_context.Products);
        _context.SaveChanges();
    }

    [Given(@"a product exists with quantity (.*)")]
    public void GivenAProductExistsWithQuantity(int quantity)
    {
        _context.Products.RemoveRange(_context.Products);
        _context.Products.Add(new Product { Quantity = quantity, Price = 0 });
        _context.SaveChanges();
    }

    [Given(@"there are (.*) screens produced")]
    public void GivenThereAreScreensProduced(int quantity)
    {
        _context.Products.Add(new Product { Quantity = quantity });
        _context.SaveChanges();
    }

    [Given(@"(.*) are reserved for orders waiting payment or collection")]
    public void GivenReservedScreens(int reserved)
    {
        var waitingPayment = new OrderStatus { Id = 1, Status = Status.WaitingForPayment };
        var waitingCollection = new OrderStatus { Id = 2, Status = Status.WaitingForCollection };
        _context.OrderStatuses.AddRange(waitingPayment, waitingCollection);
        _context.SaveChanges();

        var reservedHalf = reserved / 2;
        _context.ScreenOrders.Add(new ScreenOrder { Quantity = reservedHalf, OrderStatus = waitingPayment });
        _context.ScreenOrders.Add(new ScreenOrder { Quantity = reserved - reservedHalf, OrderStatus = waitingCollection });

        _context.SaveChanges();
    }

    [Given(@"a product exists with stock and valid equipment parameters")]
    public void GivenAProductExistsWithStockAndValidEquipmentParameters()
    {
        _context.Products.Add(new Product { Quantity = 50 });
        _context.EquipmentParameters.Add(new EquipmentParameters
        {
            InputSandKg = 10,
            InputCopperKg = 5,
            OutputScreens = 50
        });

        _context.PurchaseOrders.Add(new PurchaseOrder
        {
             EquipmentOrder = true,
             OrderStatusId = 8,
             UnitPrice = 1000,
             Quantity = 1,
             BankAccountNumber = "123456789",   // <-- required
            Origin = "Test"
            });

        // Setup mock interface
        _mockMaterialService.Setup(x => x.GetAverageCostPerKgAsync("sand")).ReturnsAsync(20m);
        _mockMaterialService.Setup(x => x.GetAverageCostPerKgAsync("copper")).ReturnsAsync(10m);

        _context.SaveChanges();
    }

    [Given(@"there are (.*) total screens and (.*) are reserved")]
    public void GivenTotalAndReservedScreens(int total, int reserved)
    {
        var waitingPayment = new OrderStatus { Id = 1, Status = Status.WaitingForPayment };
        _context.Products.Add(new Product { Quantity = total });
        _context.OrderStatuses.Add(waitingPayment);
        _context.ScreenOrders.Add(new ScreenOrder { Quantity = reserved, OrderStatus = waitingPayment });
        _context.SaveChanges();
    }

    [When(@"I add (.*) screens")]
    public async Task WhenIAddScreens(int quantity)
    {
        _boolResult = await _productService.AddScreensAsync(quantity);
    }

    [When(@"I consume (.*) screens")]
    public async Task WhenIConsumeScreens(int quantity)
    {
        _boolResult = await _productService.ConsumeScreensAsync(quantity);
    }

    [When(@"I check available stock")]
    public async Task WhenICheckAvailableStock()
    {
        _intResult = await _productService.GetAvailableStockAsync();
    }

    [When(@"I update the unit price")]
    public async Task WhenIUpdateTheUnitPrice()
    {
        _boolResult = await _productService.UpdateUnitPriceAsync();
    }

    [When(@"I request a stock summary")]
    public async Task WhenIRequestAStockSummary()
    {
        _stockSummary = await _productService.GetStockSummaryAsync();
    }

    [Then(@"a new product should exist with quantity (.*) and price (.*)")]
    public void ThenANewProductShouldExist(int quantity, int price)
    {
        var product = _context.Products.FirstOrDefault();
        product.Should().NotBeNull();
        product!.Quantity.Should().Be(quantity);
        product.Price.Should().Be(price);
        _boolResult.Should().BeTrue();
    }

    [Then(@"the product quantity should be (.*)")]
    public void ThenTheProductQuantityShouldBe(int expected)
    {
        _context.Products.First().Quantity.Should().Be(expected);
    }

    [Then(@"the remaining quantity should be (.*)")]
    public void ThenTheRemainingQuantityShouldBe(int expected)
    {
        _context.Products.First().Quantity.Should().Be(expected);
    }

    [Then(@"the operation should return false")]
    public void ThenTheOperationShouldReturnFalse()
    {
        _boolResult.Should().BeFalse();
    }

    [Then(@"the result should be (.*)")]
    public void ThenTheResultShouldBe(int expected)
    {
        _intResult.Should().Be(expected);
    }

    [Then(@"the price should be greater than 0")]
    public void ThenThePriceShouldBeGreaterThanZero()
    {
        var product = _context.Products.First();
        product.Price.Should().BeGreaterThan(0);
        _boolResult.Should().BeTrue();
    }

    [Then(@"total produced should be (.*)")]
    public void ThenTotalProducedShouldBe(int total)
    {
        _stockSummary.totalProduced.Should().Be(total);
    }

    [Then(@"reserved should be (.*)")]
    public void ThenReservedShouldBe(int reserved)
    {
        _stockSummary.reserved.Should().Be(reserved);
    }

    [Then(@"available should be (.*)")]
    public void ThenAvailableShouldBe(int available)
    {
        _stockSummary.available.Should().Be(available);
    }

    [AfterScenario]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
