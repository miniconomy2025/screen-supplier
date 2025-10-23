using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using ScreenProducerAPI.IntegrationTests.Fixtures;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenProducerAPI.IntegrationTests.Tests.Services
{
    [TestFixture]
    public class QueueProcessingBackgroundServiceTests
    {
        private CustomWebApplicationFactory _factory = null!;
        private IServiceScope _scope = null!;
        private Mock<IPurchaseOrderQueueService> _mockQueueService = null!;
        private IOptionsMonitor<QueueSettingsConfig> _config = null!;
        private ILogger<QueueProcessingBackgroundService> _logger = null!;
        private QueueProcessingBackgroundService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = new CustomWebApplicationFactory();
            _scope = _factory.Services.CreateScope();

            _mockQueueService = new Mock<IPurchaseOrderQueueService>();
            _config = _scope.ServiceProvider.GetRequiredService<IOptionsMonitor<QueueSettingsConfig>>();
            _logger = _scope.ServiceProvider.GetRequiredService<ILogger<QueueProcessingBackgroundService>>();

            var mockConfig = new Mock<IOptionsMonitor<QueueSettingsConfig>>();
            mockConfig.Setup(c => c.CurrentValue)
                .Returns(new QueueSettingsConfig { ProcessingIntervalSeconds = 1 });
            _config = mockConfig.Object;

            var spMock = new Mock<IServiceProvider>();
            spMock.Setup(sp => sp.GetService(typeof(IPurchaseOrderQueueService)))
                  .Returns(_mockQueueService.Object);

            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider).Returns(spMock.Object);

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                               .Returns(scopeFactoryMock.Object);

            _service = new QueueProcessingBackgroundService(serviceProviderMock.Object, _config, _logger);
        }

        [TearDown]
        public void TearDown()
        {
            _scope?.Dispose();
            _factory?.Dispose();
        }

        [Test]
        public async Task ExecuteAsync_Should_Call_PopulateQueueFromDatabaseAsync_Once()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await _service.StartAsync(cts.Token);

            await Task.Delay(500);

            _mockQueueService.Verify(q => q.PopulateQueueFromDatabaseAsync(), Times.Once);

            await _service.StopAsync(CancellationToken.None);
        }

        [Test]
        public async Task ExecuteAsync_Should_Call_ProcessQueueAsync_Periodically()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _service.StartAsync(cts.Token);


            await Task.Delay(2500);

            _mockQueueService.Verify(q => q.ProcessQueueAsync(),
                Times.AtLeast(2));

            await _service.StopAsync(CancellationToken.None);
        }

        [Test]
        public async Task ExecuteAsync_Should_Stop_When_CancellationRequested()
        {
            using var cts = new CancellationTokenSource();
            var executeTask = _service.StartAsync(cts.Token);

            cts.Cancel();
            await executeTask;

            _mockQueueService.Verify(q => q.PopulateQueueFromDatabaseAsync(), Times.AtMostOnce);
        }
    }
}
