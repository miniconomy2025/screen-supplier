namespace ScreenProducerAPI.Services;


public class SimulationTimeProvider
{
    private readonly SimulationTimeService _simulationTimeService;

    public SimulationTimeProvider(SimulationTimeService simulationTimeService)
    {
        _simulationTimeService = simulationTimeService;
    }

    public DateTime Now => _simulationTimeService.IsSimulationRunning()
        ? DateTime.SpecifyKind(_simulationTimeService.GetSimulationDateTime(), DateTimeKind.Utc)
        : DateTime.UtcNow;

    public bool IsSimulationRunning => _simulationTimeService.IsSimulationRunning();
}