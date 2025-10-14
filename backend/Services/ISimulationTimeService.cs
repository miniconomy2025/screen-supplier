namespace ScreenProducerAPI.Services;

public interface ISimulationTimeService : IDisposable
{
    Task<bool> StartSimulationAsync(long unixEpochStart, bool isResuming);
    int GetCurrentSimulationDay();
    DateTime GetSimulationDateTime();
    bool IsSimulationRunning();
    TimeSpan GetTimeUntilNextDay();
    void StopSimulation();
    Task DestroySimulation();
}