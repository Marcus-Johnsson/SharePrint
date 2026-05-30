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
            .WithName("UpdateListing")
            .Produces<ListingContracts.ListingDetail>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
    private class Request
    {
        public string?  Title        { get; set; }
        public string?  Description  { get; set; }
        public decimal? Price        { get; set; }
        public bool?    DownloadAble { get; set; }
        public bool?    PrintAble    { get; set; }
        public IFormFile?           Thumbnail     { get; set; }
        public IFormFileCollection? GalleryImages { get; set; }
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

        var user = (await users.GetUserAsync(context.User))!;
        if (listing.SellerId != user.Id) return TypedResults.Forbid();

        // Text fields — null = skip
        if (req.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return TypedResults.Problem("Title required.", statusCode: 400);
            listing.Title = req.Title;
        }
        if (req.Description is not null) listing.Description = req.Description;
        if (req.Price is not null)
        {
            if (req.Price <= 0) return TypedResults.Problem("Price must be > 0.", statusCode: 400);
            listing.Price = req.Price.Value;
        }

        // Purchase types — null = keep current
        var newDownloadAble = req.DownloadAble ?? listing.DownloadAble;
        var newPrintAble    = req.PrintAble    ?? listing.PrintAble;
        if (!newDownloadAble && !newPrintAble)
            return TypedResults.Problem("Minst ett val av köp val", statusCode: 400);
        listing.DownloadAble = newDownloadAble;
        listing.PrintAble    = newPrintAble;

        // Pictures — null = skip
        var replaceThumb   = req.Thumbnail is not null && req.Thumbnail.Length > 0;
        var galleryFiles   = req.GalleryImages?.GetFiles("galleryImages") ?? [];
        var replaceGallery = galleryFiles.Count > 0;

        if (replaceThumb && !await ValidateImageAsync(req.Thumbnail!))
            return TypedResults.Problem("Thumbnail invalid (mime/size/magic).", statusCode: 400);
        if (replaceGallery)
        {
            if (galleryFiles.Count > 5)
                return TypedResults.Problem("Gallery must contain 1 to 5 images.", statusCode: 400);
            foreach (var img in galleryFiles)
                if (!await ValidateImageAsync(img))
                    return TypedResults.Problem("Gallery image invalid (mime/size/magic).", statusCode: 400);
        }

        var oldThumbKey   = listing.MarketPictureKey;
        var oldGalleryKeys = listing.GalleryImages.Select(g => g.StorageKey).ToList();
        var newKeys        = new List<string>();
        string? newThumbKey = null;
        var newGalleryKeys  = new List<string>();

        try
        {
            if (replaceThumb)
            {
                await using var s = req.Thumbnail!.OpenReadStream();
                newThumbKey = await pictureStorage.SaveAsync(s, req.Thumbnail.ContentType);
                newKeys.Add(newThumbKey);
            }
            if (replaceGallery)
            {
                foreach (var img in galleryFiles)
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
                    .Select((k, i) => new ListingImage { ListingId = listing.Id, StorageKey = k, Order = i })
                    .ToList();
                db.ListingImages.AddRange(newRows);
                listing.GalleryImages = newRows;
            }

            await db.SaveChangesAsync();

            if (replaceThumb && oldThumbKey != "")
                try { await pictureStorage.DeleteAsync(oldThumbKey); } catch { }
            if (replaceGallery)
                foreach (var k in oldGalleryKeys)
                    try { await pictureStorage.DeleteAsync(k); } catch { }

            return TypedResults.Ok(ListingEndpoints.ToDetail(listing, user.UserName ?? "Unknown"));
        }
        catch
        {
            foreach (var k in newKeys)
                try { await pictureStorage.DeleteAsync(k); } catch { }
            throw;
        }
    }

    private static async Task<bool> ValidateImageAsync(IFormFile file)
    {
        if (file.Length > PictureValidator.DefaultMaxBytes) return false;
        var buffer = new byte[Math.Min(file.Length, 16)];
        await using var s = file.OpenReadStream();
        var read = await s.ReadAsync(buffer);
        return PictureValidator.IsAllowedImage(buffer.AsSpan(0, read), file.ContentType,
            PictureValidator.DefaultMaxBytes, out _);
    }
}
