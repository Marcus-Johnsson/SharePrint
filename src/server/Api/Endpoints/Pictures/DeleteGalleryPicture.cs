using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Api.Endpoints.Seller;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Pictures;

public class DeleteGalleryPicture : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/listings/{id}/gallery/{imageId}", Handler)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("DeleteGalleryPicture")
            .Produces<ListingContracts.ListingDetail>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handler(
        [FromRoute] Guid id,
        [FromRoute] Guid imageId,
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

        var img = listing.GalleryImages.FirstOrDefault(i => i.Id == imageId);
        if (img is null) return TypedResults.NotFound();
        if (listing.GalleryImages.Count <= 1)
            return TypedResults.Problem("Minst en bild behövs finnas", statusCode: 400);

        var key = img.StorageKey;
        listing.GalleryImages.Remove(img);
        await db.SaveChangesAsync();

        try { await pictureStorage.DeleteAsync(key); } catch { /* log */ }

        return TypedResults.Ok(
            ListingEndpoints.ToDetail(listing, user.UserName ?? "unknown"));
    }
}
