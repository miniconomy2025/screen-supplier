namespace ScreenProducerAPI.Models.Responses;

public class SimulationStatusResponse
{
    public bool IsRunning { get; set; }
    public int CurrentDay { get; set; }
    public DateTime SimulationDateTime { get; set; }
    public TimeSpan TimeUntilNextDay { get; set; }
}
