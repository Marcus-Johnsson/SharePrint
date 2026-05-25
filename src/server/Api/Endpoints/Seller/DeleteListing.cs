using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Seller;

public class DeleteListing : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/listings/{id}", Handler)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("DeleteListing");
    }

    private static async Task<IResult> Handler(
        [FromRoute] Guid id,
        HttpContext context,
        IFileStorage fileStorage,
        IPictureStorage pictureStorage,
        UserManager<User> users,
        SharePrintDbContext db)
    {
        var listing = await db.Listings
            .Include(l => l.GalleryImages)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) return Results.NotFound();

        var user = (await users.GetUserAsync(context.User))!;
        if (listing.SellerId != user.Id) return Results.Forbid();

        var productKey   = listing.StorageKey;
        var thumbKey     = listing.MarketPictureKey;
        var galleryKeys  = listing.GalleryImages.Select(g => g.StorageKey).ToList();

        db.Listings.Remove(listing);
        await db.SaveChangesAsync();

        try { await fileStorage.DeleteAsync(productKey); }       catch { /* log */ }
        try { await pictureStorage.DeleteAsync(thumbKey); }      catch { /* log */ }
        foreach (var key in galleryKeys)
            try { await pictureStorage.DeleteAsync(key); }       catch { /* log */ }

        return Results.NoContent();
    }
}
