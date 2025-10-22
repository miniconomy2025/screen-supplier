namespace ScreenProducerAPI.Services;


public class SimulationTimeProvider : ISimulationTimeProvider
{
    private readonly ISimulationTimeService _simulationTimeService;

    public SimulationTimeProvider(ISimulationTimeService simulationTimeService)
    {
        _simulationTimeService = simulationTimeService;
    }

    public virtual DateTime Now => _simulationTimeService.IsSimulationRunning()
        ? DateTime.SpecifyKind(_simulationTimeService.GetSimulationDateTime(), DateTimeKind.Utc)
        : DateTime.UtcNow;

    public virtual bool IsSimulationRunning => _simulationTimeService.IsSimulationRunning();
}