using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenProducerAPI.Models;

[Table("purchase_orders")]
public class PurchaseOrder
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("order_id")]
    public int OrderID { get; set; }

    [Column("shipment_id")]
    public int? ShipmentID { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("quantity_delivered")]
    public int QuantityDelivered { get; set; } = 0;

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("unit_price")]
    public int UnitPrice { get; set; }

    [Column("seller_bank_account")]
    public string BankAccountNumber { get; set; }

    [Column("origin")]
    public string Origin { get; set; }

    [Column("order_shipping_price")]
    public int OrderShippingPrice { get; set; } = 0;

    [Column("shipper_bank_account")]
    public string? ShipperBankAccout { get; set; }

    [Column("status_id")]
    public int OrderStatusId { get; set; }
    public OrderStatus OrderStatus { get; set; }

    [Column("raw_materials_id")]
    public int? RawMaterialId { get; set; }
    public Material? RawMaterial { get; set; }

    [Column("equipment_order")]
    public bool? EquipmentOrder { get; set; }
}
