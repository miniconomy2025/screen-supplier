namespace ScreenProducerAPI.Models.Responses;

public class SimulationStartResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public int CurrentDay { get; set; }
    public DateTime SimulationDateTime { get; set; }
}