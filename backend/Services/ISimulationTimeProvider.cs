namespace ScreenProducerAPI.Services
{
    public interface ISimulationTimeProvider
    {
        DateTime Now { get; }
        bool IsSimulationRunning { get; }
    }
}
