namespace ScreenProducerAPI.Models;

public class MaterialStatistics
{
    public int DailyConsumption { get; init; }
    public int ReorderPoint { get; init; }

    public MaterialStatistics()
    {
        DailyConsumption = 0;
        ReorderPoint = 0;
    }
}
