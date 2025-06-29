using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenProducerAPI.Models;

[Table("equipment")]
public class Equipment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("input_sand_kg")]
    public int InputSandKg { get; set; }

    [Column("input_copper_kg")]
    public int InputCopperKg { get; set; }

    [Column("output_screens_day")]
    public int OutputScreens { get; set; }

    [Column("is_producing")]
    public bool IsProducing { get; set; }

    [Column("is_available")]
    public bool IsAvailable { get; set; }

    [Column("purchase_orders_id")]
    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}
