namespace ScreenProducerAPI.Services.SupplierService.Hand.Models;

public class HandSimulationStatus
{
    public bool IsRunning { get; set; } = false;
    public long EpochStartTime { get; set; }
}
