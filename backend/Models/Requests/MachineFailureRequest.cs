namespace ScreenProducerAPI.Models.Requests;

public class MachineFailureRequest
{
    public string MachineName { get; set; } = string.Empty;
    public int FailureQuantity { get; set; }
    public DateTime SimulationDate { get; set; }
    public TimeSpan SimulationTime { get; set; }
}
