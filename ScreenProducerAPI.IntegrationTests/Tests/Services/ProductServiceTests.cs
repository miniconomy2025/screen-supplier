using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ScreenProducerAPI.IntegrationTests.Fixtures;
using ScreenProducerAPI.Models;
using ScreenProducerAPI.ScreenDbContext;
using ScreenProducerAPI.Services;
using System.Linq;
using System.Threading.Tasks;

namespace ScreenProducerAPI.IntegrationTests.Tests.Services
{
    [TestFixture]
    public class ProductServiceTests
    {
        private CustomWebApplicationFactory _factory = null!;
        private IServiceScope _scope = null!;
        private ScreenContext _context = null!;
        private ProductService _service = null!;
        private IMaterialService _materialService = null!;

        [SetUp]
        public async Task SetUp()
        {
            _factory = new CustomWebApplicationFactory();
            _scope = _factory.Services.CreateScope();

            _context = _scope.ServiceProvider.GetRequiredService<ScreenContext>();
            _materialService = _scope.ServiceProvider.GetRequiredService<IMaterialService>();
            _service = new ProductService(_context, _materialService);

            // Clean up database before each test
            _context.Products.RemoveRange(_context.Products);
            _context.ScreenOrders.RemoveRange(_context.ScreenOrders);
            _context.PurchaseOrders.RemoveRange(_context.PurchaseOrders);
            await _context.SaveChangesAsync();
        }

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _factory?.Dispose();
        }

        [Test]
        public async Task AddScreensAsync_Should_Add_New_Product_When_None_Exists()
        {
            // Act
            var result = await _service.AddScreensAsync(100);

            // Assert
            result.Should().BeTrue();
            var product = await _context.Products.FirstOrDefaultAsync();
            product.Should().NotBeNull();
            product!.Quantity.Should().Be(100);
            product.Price.Should().Be(0);
        }

        [Test]
        public async Task AddScreensAsync_Should_Increment_Existing_Quantity()
        {
            // Arrange
            _context.Products.Add(new Product { Quantity = 50, Price = 0 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.AddScreensAsync(25);

            // Assert
            result.Should().BeTrue();
            var product = await _context.Products.FirstAsync();
            product.Quantity.Should().Be(75);
        }

        [Test]
        public async Task ConsumeScreensAsync_Should_Decrease_Quantity_When_Enough_Stock()
        {
            // Arrange
            _context.Products.Add(new Product { Quantity = 100, Price = 0 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ConsumeScreensAsync(30);

            // Assert
            result.Should().BeTrue();
            var product = await _context.Products.FirstAsync();
            product.Quantity.Should().Be(70);
        }

        [Test]
        public async Task ConsumeScreensAsync_Should_ReturnFalse_When_Insufficient_Stock()
        {
            // Arrange
            _context.Products.Add(new Product { Quantity = 20, Price = 0 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ConsumeScreensAsync(50);

            // Assert
            result.Should().BeFalse();
            var product = await _context.Products.FirstAsync();
            product.Quantity.Should().Be(20);
        }

        [Test]
        public async Task GetAvailableStockAsync_Should_Exclude_Reserved_Screens()
        {
            // Arrange
            var product = new Product { Quantity = 100 };
            _context.Products.Add(product);

            var waitingPaymentStatus = new OrderStatus { Id = 1, Status = Status.WaitingForPayment };
            var waitingCollectionStatus = new OrderStatus { Id = 2, Status = Status.WaitingForCollection };
            var completedStatus = new OrderStatus { Id = 3, Status = Status.Completed };

            _context.OrderStatuses.AddRange(waitingPaymentStatus, waitingCollectionStatus, completedStatus);
            await _context.SaveChangesAsync();

            _context.ScreenOrders.AddRange(
                new ScreenOrder { Quantity = 10, OrderStatusId = waitingPaymentStatus.Id },
                new ScreenOrder { Quantity = 15, OrderStatusId = waitingCollectionStatus.Id },
                new ScreenOrder { Quantity = 20, OrderStatusId = completedStatus.Id }
            );
            await _context.SaveChangesAsync();

            // Act
            var available = await _service.GetAvailableStockAsync();

            // Assert
            available.Should().Be(100 - (10 + 15)); // Exclude only reserved screens
        }

        [Test]
        public async Task GetStockSummaryAsync_Should_Return_Correct_Totals()
        {
            // Arrange
            var product = new Product { Quantity = 200 };
            _context.Products.Add(product);

            var waitingPaymentStatus = new OrderStatus { Id = 1, Status = Status.WaitingForPayment };
            var waitingCollectionStatus = new OrderStatus { Id = 2, Status = Status.WaitingForCollection };
            _context.OrderStatuses.AddRange(waitingPaymentStatus, waitingCollectionStatus);
            await _context.SaveChangesAsync();

            _context.ScreenOrders.AddRange(
                new ScreenOrder { Quantity = 30, OrderStatusId = waitingPaymentStatus.Id },
                new ScreenOrder { Quantity = 20, OrderStatusId = waitingCollectionStatus.Id }
            );
            await _context.SaveChangesAsync();

            // Act
            var (totalProduced, reserved, available) = await _service.GetStockSummaryAsync();

            // Assert
            totalProduced.Should().Be(200);
            reserved.Should().Be(50);
            available.Should().Be(150);
        }

        [Test]
        public async Task GetProductAsync_Should_Return_Product_If_Exists()
        {
            // Arrange
            var product = new Product { Quantity = 99, Price = 10 };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetProductAsync();

            // Assert
            result.Should().NotBeNull();
            result!.Quantity.Should().Be(99);
            result.Price.Should().Be(10);
        }

        [Test]
        public async Task GetProductsAsync_Should_Return_All_Products()
        {
            // Arrange
            _context.Products.AddRange(
                new Product { Quantity = 10 },
                new Product { Quantity = 20 }
            );
            await _context.SaveChangesAsync();

            // Act
            var results = await _service.GetProductsAsync();

            // Assert
            results.Should().HaveCount(2);
        }
    }
}
