using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SharePrint.Domain;

namespace SharePrint.Infrastructure.Persistence;

public class SharePrintDbContext : IdentityDbContext<User>
{
    public SharePrintDbContext(DbContextOptions<SharePrintDbContext> options) : base(options)
    {
        
    }
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<DownloadGrant> DownloadGrants => Set<DownloadGrant>();
    
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Order>().HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
        b.Entity<OrderItem>().HasOne(i => i.grant).WithOne().HasForeignKey<DownloadGrant>(g => g.OrderItemId);
        b.Entity<Listing>().Property(l => l.Price).HasColumnType("decimal(18,2)");
        b.Entity<Order>().Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");
        b.Entity<OrderItem>().Property(o => o.UnitPrice).HasColumnType("decimal(18,2)");
    }
}