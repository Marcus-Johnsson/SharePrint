using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Application.Listings;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Pictures;

public class PutReplaceMarketPicture : IEndpoint
{
    
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/pictures/replace/{id}", Handler)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("ReplaceMarketPicture");
    }

    private static async Task<IResult> Handler( // Error messages need some work... validation need to return either message for what error or split validation to three separate
        [FromHeader] Guid id,
        HttpContext context,
        HttpRequest request,
        IPictureStorage pictureStorage,
        UserManager<User> users,
        SharePrintDbContext db)
    {
        if (!request.HasFormContentType)
            return Results.Problem("Filens typ hittades inte.");

        var form = await request.ReadFormAsync();
        var thumb = form.Files["thumbnail"];

        if (thumb is null || thumb.Length == 0)
            return Results.Problem("Ingen bild hittades");
        if (!await ValidateImageAsync(thumb))
            return Results.Problem("Bild data ej godkänd (mime/storlek/magic).", statusCode: 400);
        
        var listing = await db.Listings.Include(x => x.GalleryImages).FirstOrDefaultAsync(x => x.Id == id);
        if (listing is null) return Results.NotFound();
        var user = (await users.GetUserAsync(context.User))!;
        if (listing.SellerId != user.Id) return Results.Forbid();

        string newKey;
        await using var stream = thumb.OpenReadStream();
            newKey = await pictureStorage.SaveAsync(stream, thumb.ContentType);
            
        var oldKey = listing.MarketPictureKey;
        listing.MarketPictureKey = newKey;
        try
        {
            await db.SaveChangesAsync();
        }
        catch
        {
            try { await pictureStorage.DeleteAsync(newKey); } catch { }
            throw;
        }
        try { await pictureStorage.DeleteAsync(oldKey); } catch {  }
        return Results.Ok(ToDetail(listing, user.UserName ?? "unknown"));
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
            l.Status.ToString());
}