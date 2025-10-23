using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Models.Configuration;
using ScreenProducerAPI.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
public sealed class QueueProcessingBackgroundServiceStepDefinitions
{
    private Mock<IServiceScopeFactory> _mockScopeFactory = null!;
    private Mock<IServiceScope> _mockScope = null!;
    private Mock<IServiceProvider> _mockServiceProvider = null!;
    private Mock<IPurchaseOrderQueueService> _mockQueueService = null!;
    private Mock<IOptionsMonitor<QueueSettingsConfig>> _mockConfig = null!;
    private Mock<ILogger<QueueProcessingBackgroundService>> _mockLogger = null!;
    private QueueProcessingBackgroundService _backgroundService = null!;
    private CancellationTokenSource _cts = null!;
    private TimeSpan _testDelay;
    private Task _executionTask = null!;

    [BeforeScenario]
    public void Setup()
    {
        _mockQueueService = new Mock<IPurchaseOrderQueueService>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockConfig = new Mock<IOptionsMonitor<QueueSettingsConfig>>();

        // Default interval
        _mockConfig.Setup(c => c.CurrentValue)
            .Returns(new QueueSettingsConfig { ProcessingIntervalSeconds = 1 });

        // Setup service resolution for IServiceScopeFactory
        _mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockScopeFactory.Object);

        // Setup service resolution for IPurchaseOrderQueueService
        _mockServiceProvider.Setup(p => p.GetService(typeof(IPurchaseOrderQueueService)))
            .Returns(_mockQueueService.Object);

        _mockScopeFactory.Setup(f => f.CreateScope())
            .Returns(_mockScope.Object);

        _mockScope.Setup(s => s.ServiceProvider)
            .Returns(_mockServiceProvider.Object);

        _backgroundService = new QueueProcessingBackgroundService(_mockServiceProvider.Object, _mockConfig.Object, _mockLogger.Object);
        _cts = new CancellationTokenSource();
    }

    [Given(@"a queue processing interval of (.*) seconds")]
    public void GivenAQueueProcessingIntervalOfSeconds(int interval)
    {
        _mockConfig.Setup(c => c.CurrentValue)
            .Returns(new QueueSettingsConfig { ProcessingIntervalSeconds = interval });
    }

    [When(@"the background service starts")]
    public void WhenTheBackgroundServiceStarts()
    {
        _executionTask = _backgroundService.StartAsync(_cts.Token);
    }

    [When(@"the background service runs for (.*) milliseconds")]
    public async Task WhenTheBackgroundServiceRunsForMilliseconds(int milliseconds)
    {
        await Task.Delay(milliseconds, CancellationToken.None);
    }

    [When(@"I stop the background service")]
    public async Task WhenIStopTheBackgroundService()
    {
        _cts.Cancel();
        await _backgroundService.StopAsync(CancellationToken.None);
    }

    [Then(@"PopulateQueueFromDatabaseAsync should be called once")]
    public void ThenPopulateQueueFromDatabaseAsyncShouldBeCalledOnce()
    {
        _mockQueueService.Verify(q => q.PopulateQueueFromDatabaseAsync(), Times.Once());
    }

    [Then(@"ProcessQueueAsync should be called at least (.*) times")]
    public void ThenProcessQueueAsyncShouldBeCalledAtLeastTimes(int minTimes)
    {
        _mockQueueService.Verify(q => q.ProcessQueueAsync(), Times.AtLeast(minTimes));
    }

    [Then(@"ProcessQueueAsync should handle exceptions and continue running")]
    public async Task ThenProcessQueueAsyncShouldHandleExceptionsAndContinueRunning()
    {
        int callCount = 0;
        _mockQueueService.Setup(q => q.ProcessQueueAsync())
            .Callback(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("Test exception");
            })
            .Returns(Task.CompletedTask);

        _executionTask = _backgroundService.StartAsync(_cts.Token);
        await Task.Delay(500, CancellationToken.None); // Increased delay for reliability
        _cts.Cancel();

        _mockQueueService.Verify(q => q.ProcessQueueAsync(), Times.AtLeast(2));
    }

    [AfterScenario]
    public async Task Cleanup()
    {
        _cts.Cancel();
        if (_executionTask != null)
        {
            try
            {
                await _backgroundService.StopAsync(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation exceptions during cleanup
            }
        }
        _cts.Dispose();
    }
}