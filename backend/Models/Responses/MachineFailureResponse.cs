namespace ScreenProducerAPI.Models.Responses;

public class MachineFailureResponse
{
    public bool Success { get; set; }
    public int FailedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}