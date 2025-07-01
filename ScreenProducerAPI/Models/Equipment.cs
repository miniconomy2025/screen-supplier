using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenProducerAPI.Models;

[Table("equipment")]
public class Equipment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("parameters_id")]
    public int ParametersID { get; set; }
    public EquipmentParameters? EquipmentParameters { get; set; }

    [Column("is_producing")]
    public bool IsProducing { get; set; }

    [Column("is_available")]
    public bool IsAvailable { get; set; }

    [Column("purchase_orders_id")]
    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}
