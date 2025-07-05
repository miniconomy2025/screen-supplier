using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenProducerAPI.Models;

[Table("bank_details")]
public class BankDetails
{
    [Key]
    [Column("account_number")]
    public string AccountNumber { get; set; } = null!;
}
