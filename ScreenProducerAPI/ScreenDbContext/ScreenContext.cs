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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Equipment>()
            .HasOne(e => e.EquipmentParameters)
            .WithMany()
            .HasForeignKey(e => e.ParametersID)
            .HasPrincipalKey(ep => ep.Id);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.RawMaterial)
            .WithMany()
            .HasForeignKey(po => po.RawMaterialId)
            .HasPrincipalKey(m => m.Id);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.OrderStatus)
            .WithMany()
            .HasForeignKey(po => po.OrderStatusId)
            .HasPrincipalKey(os => os.Id);

        modelBuilder.Entity<ScreenOrder>()
            .HasOne(so => so.OrderStatus)
            .WithMany()
            .HasForeignKey(so => so.OrderStatusId)
            .HasPrincipalKey(os => os.Id);

        modelBuilder.Entity<ScreenOrder>()
            .HasOne(so => so.Product)
            .WithMany()
            .HasForeignKey(so => so.ProductId)
            .HasPrincipalKey(p => p.Id);

        modelBuilder.Entity<Equipment>()
            .HasOne(e => e.PurchaseOrder)
            .WithMany()
            .HasForeignKey(e => e.PurchaseOrderId)
            .HasPrincipalKey(po => po.Id);
    }
}
