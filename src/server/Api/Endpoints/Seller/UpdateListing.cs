using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Application.Listings;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Seller;

public class UpdateListing : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/listings/{id}", Handler)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("UpdateListing");
    }

    private static async Task<IResult> Handler(
        [FromRoute] Guid id,
        HttpContext context,
        HttpRequest request,
        IPictureStorage pictureStorage,
        UserManager<User> users,
        SharePrintDbContext db)
    {
        if (!request.HasFormContentType)
            return Results.Problem("Multipart form required.", statusCode: 400);

        var listing = await db.Listings
            .Include(l => l.GalleryImages)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) return Results.NotFound();

        var user = (await users.GetUserAsync(context.User))!;
        if (listing.SellerId != user.Id) return Results.Forbid();

        var form = await request.ReadFormAsync();

        // ---------- Text fields ----------
        if (form.ContainsKey("title"))
        {
            var title = form["title"].ToString();
            if (string.IsNullOrWhiteSpace(title))
                return Results.Problem("Title required.", statusCode: 400);
            listing.Title = title;
        }

        if (form.ContainsKey("description"))
            listing.Description = form["description"].ToString();

        if (form.ContainsKey("price"))
        {
            if (!decimal.TryParse(form["price"],
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var price) || price <= 0)
                return Results.Problem("Price must be > 0.", statusCode: 400);
            listing.Price = price;
        }

        // ---------- Purchase type fields ----------
        var newDownloadAble = bool.TryParse(form["downloadAble"], out var ndl) ? ndl : listing.DownloadAble;
        var newPrintAble    = bool.TryParse(form["printAble"],    out var npr) ? npr : listing.PrintAble;
        if (!newDownloadAble && !newPrintAble)
            return Results.Problem("Minst ett val av köp val", statusCode: 400);
        listing.DownloadAble = newDownloadAble;
        listing.PrintAble    = newPrintAble;

        // ---------- Picture fields ----------
        var thumb = form.Files["thumbnail"];
        var gallery = form.Files.GetFiles("galleryImages");

        var replaceThumb = thumb is not null && thumb.Length > 0;
        var replaceGallery = gallery.Count > 0;

        if (replaceThumb && !await ValidateImageAsync(thumb!))
            return Results.Problem("Thumbnail invalid (mime/size/magic).", statusCode: 400);

        if (replaceGallery)
        {
            if (gallery.Count > 5)
                return Results.Problem("Gallery must contain 1 to 5 images.", statusCode: 400);
            foreach (var img in gallery)
                if (!await ValidateImageAsync(img))
                    return Results.Problem("Gallery image invalid (mime/size/magic).", statusCode: 400);
        }

        // Capture old keys before mutation so post-commit cleanup can delete them.
        var oldThumbKey = listing.MarketPictureKey;
        var oldGalleryKeys = listing.GalleryImages.Select(g => g.StorageKey).ToList();

        var newKeys = new List<string>();   // any blob we wrote this request; used for rollback
        string? newThumbKey = null;
        var newGalleryKeys = new List<string>();

        try
        {
            if (replaceThumb)
            {
                await using var s = thumb!.OpenReadStream();
                newThumbKey = await pictureStorage.SaveAsync(s, thumb.ContentType);
                newKeys.Add(newThumbKey);
            }

            if (replaceGallery)
            {
                foreach (var img in gallery)
                {
                    await using var s = img.OpenReadStream();
                    var key = await pictureStorage.SaveAsync(s, img.ContentType);
                    newKeys.Add(key);
                    newGalleryKeys.Add(key);
                }
            }

            if (replaceThumb) listing.MarketPictureKey = newThumbKey!;

            if (replaceGallery)
            {
                db.ListingImages.RemoveRange(listing.GalleryImages);
                var newRows = newGalleryKeys
                    .Select((k, i) => new ListingImage
                    {
                        ListingId = listing.Id,
                        StorageKey = k,
                        Order = i
                    })
                    .ToList();
                db.ListingImages.AddRange(newRows);
                listing.GalleryImages = newRows;
            }

            await db.SaveChangesAsync();

            // Commit succeeded -> retire old blobs. Best-effort: orphaned blobs
            // are tolerable, missing customer images are not.
            if (replaceThumb && oldThumbKey != "")
                try { await pictureStorage.DeleteAsync(oldThumbKey); } catch { /* log */ }
            if (replaceGallery)
                foreach (var k in oldGalleryKeys)
                    try { await pictureStorage.DeleteAsync(k); } catch { /* log */ }

            return Results.Ok(ToDetail(listing, user.UserName ?? "Unknown"));
        }
        catch
        {
            // Pre-commit failure. Roll back any blobs we wrote; old data still intact.
            foreach (var k in newKeys)
                try { await pictureStorage.DeleteAsync(k); } catch { /* log */ }
            throw;
        }
    }

    private static async Task<bool> ValidateImageAsync(IFormFile file)
    {
        if (file.Length > PictureValidator.DefaultMaxBytes) return false;
        var buffer = new byte[Math.Min(file.Length, 16)];
        await using var s = file.OpenReadStream();
        var read = await s.ReadAsync(buffer);
        return PictureValidator.IsAllowedImage(
            buffer.AsSpan(0, read),
            file.ContentType,
            PictureValidator.DefaultMaxBytes,
            out _);
    }

    private static ListingContracts.ListingDetail ToDetail(Listing l, string sellerUsername) =>
        new(
            l.Id,
            l.Title,
            l.Description,
            l.Price,
            $"/api/pictures/{l.MarketPictureKey}",
            l.GalleryImages
                .OrderBy(g => g.Order)
                .Select(g => new ListingContracts.DescriptionPicture(g.Id, $"/api/pictures/{g.StorageKey}"))
                .ToList(),
            sellerUsername,
            l.Status.ToString(),
            l.DownloadAble,
            l.PrintAble);
}
