namespace ScreenProducerAPI.Models;

public class AllMaterialStatistics
{
    public AllMaterialStatistics()
    {
        Sand = new();
        Copper = new();
    }

    public MaterialStatistics Sand { get; init; }
    public MaterialStatistics Copper { get; init; }
}
