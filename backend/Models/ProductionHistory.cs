using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenProducerAPI.Models;

[Table("production_history")]
public class ProductionHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("record_date")]
    public DateTime RecordDate { get; set; }

    [Column("sand_stock")]
    public int SandStock { get; set; }

    [Column("copper_stock")]
    public int CopperStock { get; set; }

    [Column("screens_produced")]
    public int ScreensProduced { get; set; }
}