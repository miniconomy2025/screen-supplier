using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenProducerAPI.Models;

[Table("screen_orders")]
public class ScreenOrder
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("quantity_collected")]
    public int QuantityCollected { get; set; }

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("unit_price")]
    public int UnitPrice { get; set; }

    [Column("status_id")]
    public int OrderStatusId { get; set; }
    public OrderStatus OrderStatus { get; set; } = null!;

    [Column("product_id")]
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Column("amount_paid")]
    public int? AmountPaid { get; set; }
}
