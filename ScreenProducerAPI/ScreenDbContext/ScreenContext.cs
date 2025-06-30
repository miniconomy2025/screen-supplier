using Microsoft.EntityFrameworkCore;
using ScreenProducerAPI.Models;

namespace ScreenProducerAPI.ScreenDbContext;

public class ScreenContext : DbContext
{
    public DbSet<BankDetails> BankDetails { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ScreenOrder> ScreenOrders { get; set; }
    public DbSet<Equipment> Equipment { get; set; }
    public DbSet<EquipmentParameters> EquipmentParameters { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<OrderStatus> OrderStatuses { get; set; }

    public ScreenContext(DbContextOptions<ScreenContext> options) : base(options)
    {

    }
}
