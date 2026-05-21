using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.IntegrationTests;

public class ListingImageEntityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public ListingImageEntityTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Listing_with_images()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SharePrintDbContext>();

        var listing = new Listing
        {
            SellerId = $"{Guid.NewGuid():N}",
            Title = "Test",
            Description = "d",
            Price = 1m,
            Currency = "SEK",
            StorageKey = "filekey",
            OriginalFileName = "a.bin",
            ContentType = "application/octet-stream",
            SizeBytes = 1,
            MarketPictureKey = "thumbkey",
            GalleryImages =
            {
                new ListingImage { StorageKey = "g1", Order = 0 },
                new ListingImage { StorageKey = "g2", Order = 1 },
            }
        };
        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        
        var loaded = await db.Listings
            .Include(p => p.GalleryImages)
            .FirstAsync(o => o.Id == listing.Id); 
        
        Assert.Equal("thumbkey", loaded.MarketPictureKey);
        Assert.Equal(2, loaded.GalleryImages.Count);
        Assert.Equal(new[] { "g1", "g2" }, loaded.GalleryImages.OrderBy(g => g.Order).Select(g => g.StorageKey));
    }
}