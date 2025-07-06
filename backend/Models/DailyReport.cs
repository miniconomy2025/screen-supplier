namespace ScreenProducerAPI.Models;

public class DailyReport
{
    public DateTime Date { get; set; } = DateTime.Now.Date;
    public int SandStock { get; set; }
    public int CopperStock { get; set; }
    public int SandPurchased { get; set; }
    public int CopperPurchased { get; set; }
    public int SandConsumed { get; set; }
    public int CopperConsumed { get; set; }
    public int ScreensProduced { get; set; }
    public int WorkingMachines { get; set; }
    public int ScreensSold { get; set; }
    public int Revenue { get; set; }
}
