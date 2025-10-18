using ScreenProducerAPI.Services;

namespace ScreenProducerAPI.IntegrationTests.Mocks;

/// <summary>
/// Mock implementation of LogisticsService for integration testing.
/// Simulates logistics operations without making real HTTP calls.
/// Note: LogisticsService doesn't have an interface, so we don't replace it in tests.
/// The integration tests won't call logistics endpoints that require external HTTP calls.
/// </summary>
public class MockLogisticsService
{
    // This class is a placeholder and not actually used
    // LogisticsService will be kept as-is in integration tests
    // since it doesn't have critical endpoints being tested
}
