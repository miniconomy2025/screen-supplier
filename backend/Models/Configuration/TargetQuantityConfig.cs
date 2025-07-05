namespace ScreenProducerAPI.Models.Configuration;

public class TargetQuantityConfig
{
    public int Target { get; set; }
    public int ReorderPoint { get; set; }
    public int OrderQuantity { get; set; }
}

public class TargetQuantitiesConfig
{
    public TargetQuantityConfig Sand { get; set; } = new();
    public TargetQuantityConfig Copper { get; set; } = new();
    public TargetQuantityConfig Equipment { get; set; } = new();
}

public class ReorderSettingsConfig
{
    public bool EnableAutoReorder { get; set; } = true;
    public bool EnableScreenStockCheck { get; set; } = true;
    public int MaxScreensBeforeStopOrdering { get; set; } = 1000;
}