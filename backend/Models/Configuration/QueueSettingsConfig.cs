namespace ScreenProducerAPI.Models.Configuration;

public class QueueSettingsConfig
{
    public int ProcessingIntervalSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public bool EnableQueueProcessing { get; set; } = true;
}

public class CompanyInfoConfig
{
    public string CompanyId { get; set; } = "screen-supplier";
    public string Name { get; set; } = "Screen Supplier";
}