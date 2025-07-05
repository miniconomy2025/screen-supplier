using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenProducerAPI.Models;

[Table("order_status")]
public class OrderStatus
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Status { get; set; }
}
