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
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<DownloadGrant> DownloadGrants => Set<DownloadGrant>();
    
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Order>().HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
        b.Entity<Order>().HasIndex(o => o.StripePaymentIntentId).IsUnique();
        b.Entity<OrderItem>().HasOne(i => i.Grant).WithOne().HasForeignKey<DownloadGrant>(g => g.OrderItemId);
        b.Entity<Listing>().Property(l => l.Price).HasPrecision(18, 2);
        b.Entity<Order>().Property(o => o.TotalPrice).HasPrecision(18, 2);
        b.Entity<OrderItem>().Property(o => o.UnitPrice).HasPrecision(18, 2);

        b.Entity<ListingImage>()
            .HasOne<Listing>()
            .WithMany(l => l.GalleryImages)
            .HasForeignKey(i => i.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<ListingImage>()
            .HasIndex(i => new { i.ListingId, i.Order });
    }
}
