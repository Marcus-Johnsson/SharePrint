using Microsoft.AspNetCore.Identity;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Application.Listings;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Seller;

public class ListingEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/listings", Handler)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("CreateListing");
    }

    private static async Task<IResult> Handler(
        HttpContext context,
        HttpRequest request,
        IFileStorage fileStorage,
        IPictureStorage pictureStorage,
        UserManager<User> users,
        SharePrintDbContext db)
    {
        if (!request.HasFormContentType)
            return Results.Problem("Fel form data");
        var form = await request.ReadFormAsync();

        var downloadAble = bool.TryParse(form["downloadAble"], out var dl) && dl;
        var printAble    = bool.TryParse(form["printAble"],    out var pr) && pr;
        if (!downloadAble && !printAble)
            return Results.Problem("Minst ett val av köp val", statusCode: 400);
        
        var title = form["title"].ToString();
        if (string.IsNullOrWhiteSpace(title))
            return Results.Problem("Title required.", statusCode: 400);

        if (!decimal.TryParse(form["price"], System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var price) || price <= 0)
            return Results.Problem("Price must be > 0.", statusCode: 400);

        var description = form["description"].ToString() ?? "";

        var productFile = form.Files["file"];
        if (productFile is null || productFile.Length == 0)
            return Results.Problem("Product file required.", statusCode: 400);

        var thumbnail = form.Files["thumbnail"];
        if (thumbnail is null || thumbnail.Length == 0)
            return Results.Problem("Thumbnail required.", statusCode: 400);

        var gallery = form.Files.GetFiles("galleryImages");
        if (gallery.Count is < 1 or > 5)
            return Results.Problem("Gallery must contain 1 to 5 images.", statusCode: 400);

        // Validate thumbnail.
        if (!await ValidateImageAsync(thumbnail))
            return Results.Problem("Thumbnail invalid (mime/size/magic).", statusCode: 400);

        // Validate each gallery image.
        foreach (var img in gallery)
            if (!await ValidateImageAsync(img))
                return Results.Problem("Gallery image invalid (mime/size/magic).", statusCode: 400);
        
        var user = await users.GetUserAsync(context.User)!;

        var savedFileKey = "";
        var savedThumbKey = "";
        var savedGalleryKeys = new List<string>();

        try
        {
            await using (var s = productFile.OpenReadStream())
                savedFileKey = await fileStorage.SaveAsync(s, productFile.ContentType, productFile.FileName);

            await using (var s = thumbnail.OpenReadStream())
                savedThumbKey = await pictureStorage.SaveAsync(s, thumbnail.ContentType);

            foreach (var img in gallery)
            {
                await using var s = img.OpenReadStream();
                savedGalleryKeys.Add(await pictureStorage.SaveAsync(s, img.ContentType));
            }

            var listing = new Listing
            {
                SellerId = user.Id,
                Title = title,
                Description = description,
                Price = price,
                Currency = "SEK",
                StorageKey = savedFileKey,
                OriginalFileName = productFile.FileName,
                ContentType = productFile.ContentType,
                SizeBytes = productFile.Length,
                MarketPictureKey = savedThumbKey,
                Status = ListingStatus.Active,
                DownloadAble = downloadAble,
                PrintAble = printAble,
                GalleryImages = savedGalleryKeys.Select((k, i) => new ListingImage
                {
                    StorageKey = k, Order = i
                }).ToList()
            };
                
                db.Listings.Add(listing);
                await db.SaveChangesAsync();
                
                return Results.Ok(ToDetail(listing, user.UserName ?? "Unknown"));
        }
        catch
        {
            // Best-effort cleanup.
            foreach (var k in savedGalleryKeys)
                try { await pictureStorage.DeleteAsync(k); } catch { /* log later */ }
            if (savedThumbKey != "") try { await pictureStorage.DeleteAsync(savedThumbKey); } catch { }
            if (savedFileKey != "") try { await fileStorage.DeleteAsync(savedFileKey); } catch { }
            throw;
        }
    }
    private static async Task<bool> ValidateImageAsync(IFormFile file)
    {
        if (file.Length > PictureValidator.DefaultMaxBytes) return false;
        var buffer = new byte[Math.Min(file.Length, 16)];
        await using var s = file.OpenReadStream();
        var read = await s.ReadAsync(buffer);
        return PictureValidator.IsAllowedImage(buffer.AsSpan(0, read), file.ContentType, PictureValidator.DefaultMaxBytes, out _);
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