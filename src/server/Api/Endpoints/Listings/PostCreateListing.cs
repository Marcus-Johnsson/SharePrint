using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Api.Services;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Listings;

public class PostCreateListing : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/listings", Handler)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("CreateListing")
            .Produces<ListingContracts.ListingDetail>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private class Request
    {
        public string Title        { get; set; } = "";
        public string Description  { get; set; } = "";
        public decimal Price       { get; set; }
        public bool DownloadAble   { get; set; }
        public bool PrintAble      { get; set; }
        public IFormFile?  File           { get; set; }
        public IFormFile?  Thumbnail      { get; set; }
        public IFormFileCollection? GalleryImages { get; set; }
    }

    private static async Task<IResult> Handler(
        [FromForm] Request req,
        HttpContext context,
        IFileStorage fileStorage,
        IPictureStorage pictureStorage,
        UserManager<User> users,
        SharePrintDbContext db)
    {
        if (!req.DownloadAble && !req.PrintAble)
            return TypedResults.Problem("Minst ett val av köp val", statusCode: 400);
        if (string.IsNullOrWhiteSpace(req.Title))
            return TypedResults.Problem("Title required.", statusCode: 400);
        if (req.Price <= 0)
            return TypedResults.Problem("Price must be > 0.", statusCode: 400);
        if (req.File is null || req.File.Length == 0)
            return TypedResults.Problem("Product file required.", statusCode: 400);
        if (req.Thumbnail is null || req.Thumbnail.Length == 0)
            return TypedResults.Problem("Thumbnail required.", statusCode: 400);

        var gallery = req.GalleryImages?.GetFiles("galleryImages") ?? [];
        if (gallery.Count is < 1 or > 5)
            return TypedResults.Problem("Gallery must contain 1 to 5 images.", statusCode: 400);
        if (!await ListingPictures.ValidateImageAsync(req.Thumbnail))
            return TypedResults.Problem("Thumbnail invalid (mime/size/magic).", statusCode: 400);
        foreach (var img in gallery)
            if (!await ListingPictures.ValidateImageAsync(img))
                return TypedResults.Problem("Gallery image invalid (mime/size/magic).", statusCode: 400);

        var user = (await users.GetUserAsync(context.User))!;

        var savedFileKey     = "";
        var savedThumbKey    = "";
        var savedGalleryKeys = new List<string>();

        try
        {
            await using (var s = req.File.OpenReadStream())
                savedFileKey = await fileStorage.SaveAsync(s, req.File.ContentType, req.File.FileName);
            await using (var s = req.Thumbnail.OpenReadStream())
                savedThumbKey = await pictureStorage.SaveAsync(s, req.Thumbnail.ContentType);
            foreach (var img in gallery)
            {
                await using var s = img.OpenReadStream();
                savedGalleryKeys.Add(await pictureStorage.SaveAsync(s, img.ContentType));
            }

            var listing = new Listing
            {
                SellerId         = user.Id,
                Title            = req.Title,
                Description      = req.Description,
                Price            = req.Price,
                Currency         = "SEK",
                StorageKey       = savedFileKey,
                OriginalFileName  = req.File.FileName,
                ContentType      = req.File.ContentType,
                SizeBytes        = req.File.Length,
                MarketPictureKey = savedThumbKey,
                Status           = ListingStatus.Active,
                DownloadAble     = req.DownloadAble,
                PrintAble        = req.PrintAble,
                GalleryImages    = savedGalleryKeys
                    .Select((k, i) => new ListingImage { StorageKey = k, Order = i })
                    .ToList()
            };

            db.Listings.Add(listing);
            await db.SaveChangesAsync();

            return TypedResults.Ok(ToDetail(listing, user.UserName ?? "Unknown"));
        }
        catch
        {
            foreach (var k in savedGalleryKeys)
                try { await pictureStorage.DeleteAsync(k); } catch { }
            if (savedThumbKey != "") try { await pictureStorage.DeleteAsync(savedThumbKey); } catch { }
            if (savedFileKey  != "") try { await fileStorage.DeleteAsync(savedFileKey); }    catch { }
            throw;
        }
    }

    internal static ListingContracts.ListingDetail ToDetail(Listing l, string sellerUsername) =>
        new(l.Id, l.Title, l.Description, l.Price,
            ListingPictures.PictureUrl(l.MarketPictureKey),
            l.GalleryImages.OrderBy(g => g.Order)
                .Select(g => new ListingContracts.DescriptionPicture(g.Id, ListingPictures.PictureUrl(g.StorageKey)))
                .ToList(),
            sellerUsername, l.Status.ToString(), l.DownloadAble, l.PrintAble, l.CreatedAt.DateTime.ToString(), l.LastUpdatedAt.DateTime.ToString());
}
