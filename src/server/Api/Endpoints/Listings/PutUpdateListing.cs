using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Api.Services;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Listings;

public class PutUpdateListing : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/listings/{id}", Handler)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("UpdateListing")
            .Produces<ListingContracts.ListingDetail>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private class Request
    {
        public string  Title        { get; set; } = "";
        public string  Description  { get; set; } = "";
        public decimal Price        { get; set; }
        public bool    DownloadAble { get; set; }
        public bool    PrintAble    { get; set; }

        public IFormFile? Thumbnail { get; set; }

        public List<Guid>?          KeptGalleryIds   { get; set; }
        public IFormFileCollection? NewGalleryImages { get; set; }
    }

    private static async Task<IResult> Handler(
        [FromRoute] Guid id,
        [FromForm] Request req,
        HttpContext context,
        IPictureStorage pictureStorage,
        UserManager<User> users,
        SharePrintDbContext db)
    {
        var listing = await db.Listings
            .Include(l => l.GalleryImages)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) return TypedResults.NotFound();

        var user = await users.GetUserAsync(context.User);
        if (listing.SellerId != user.Id) return TypedResults.Forbid();

        if (string.IsNullOrWhiteSpace(req.Title))
            return TypedResults.Problem("Title required.", statusCode: 400);
        if (req.Price <= 0)
            return TypedResults.Problem("Price must be > 0.", statusCode: 400);
        if (!req.DownloadAble && !req.PrintAble)
            return TypedResults.Problem("Minst ett val av köp val", statusCode: 400);

        var replaceThumb = req.Thumbnail is not null && req.Thumbnail.Length > 0;
        var newGalleryFiles = req.NewGalleryImages?.GetFiles("newGalleryImages") ?? [];
        var keptIds = req.KeptGalleryIds?.Distinct().ToList() ?? [];

        if (replaceThumb && !await ListingPictures.ValidateImageAsync(req.Thumbnail!))
            return TypedResults.Problem("Thumbnail invalid (mime/size/magic).", statusCode: 400);

        foreach (var img in newGalleryFiles)
            if (!await ListingPictures.ValidateImageAsync(img))
                return TypedResults.Problem("Gallery image invalid (mime/size/magic).", statusCode: 400);

        var existingIds = listing.GalleryImages.Select(g => g.Id).ToHashSet();
        if (keptIds.Any(p => !existingIds.Contains(p)))
            return TypedResults.Problem("Kept gallery id not found on listing.", statusCode: 400);

        var finalCount = keptIds.Count + newGalleryFiles.Count;
        if (finalCount is < 1 or > 5)
            return TypedResults.Problem("Gallery must contain 1 to 5 images.", statusCode: 400);

        var toRemove   = listing.GalleryImages.Where(g => !keptIds.Contains(g.Id)).ToList();
        var removedKeys = toRemove.Select(g => g.StorageKey).ToList();
        var oldThumbKey = listing.MarketPictureKey;

        var newKeys = new List<string>();
        string? newThumbKey = null;
        var newGalleryKeys = new List<string>();

        try
        {
            if (replaceThumb)
            {
                await using var s = req.Thumbnail!.OpenReadStream();
                newThumbKey = await pictureStorage.SaveAsync(s, req.Thumbnail.ContentType);
                newKeys.Add(newThumbKey);
            }
            foreach (var img in newGalleryFiles)
            {
                await using var s = img.OpenReadStream();
                var key = await pictureStorage.SaveAsync(s, img.ContentType);
                newKeys.Add(key);
                newGalleryKeys.Add(key);
            }

            listing.Title        = req.Title;
            listing.Description  = req.Description;
            listing.Price        = req.Price;
            listing.DownloadAble = req.DownloadAble;
            listing.PrintAble    = req.PrintAble;
            listing.LastUpdatedAt = DateTime.UtcNow;
            if (replaceThumb) listing.MarketPictureKey = newThumbKey!;

            if (toRemove.Count > 0)
                db.ListingImages.RemoveRange(toRemove);

            if (newGalleryKeys.Count > 0)
            {
                var newRows = newGalleryKeys
                    .Select(k => new ListingImage
                    {
                        ListingId = listing.Id,
                        StorageKey = k
                    })
                    .ToList();
                db.ListingImages.AddRange(newRows);
            }

            await db.SaveChangesAsync();

            if (replaceThumb && !string.IsNullOrEmpty(oldThumbKey))
                try { await pictureStorage.DeleteAsync(oldThumbKey); } catch { /* logg orphan */ }
            foreach (var k in removedKeys)
                try { await pictureStorage.DeleteAsync(k); } catch { /* logg orphan  */ }

            await db.Entry(listing).Collection(l => l.GalleryImages).LoadAsync();
            return TypedResults.Ok(PostCreateListing.ToDetail(listing, user.UserName ?? "Unknown"));
        }
        catch
        {
            foreach (var k in newKeys)
                try { await pictureStorage.DeleteAsync(k); } catch {/* logg orphan */}
            throw;
        }
    }
}
