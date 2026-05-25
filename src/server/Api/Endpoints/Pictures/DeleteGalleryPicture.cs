using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
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
            .WithName("DeleteGalleryPicture");
    }

    private static async Task<IResult> Handler(
        [FromRoute] Guid id,
        [FromRoute] Guid imageId,
        HttpContext context,
        IPictureStorage pictureStorage,
        UserManager<User> users,
        SharePrintDbContext db
    )
    {
        var list = await db.Listings.Include(
            x => x.GalleryImages).FirstOrDefaultAsync(p => p.Id == id);
        if (list is null) return Results.NotFound();

        var user = (await users.GetUserAsync(context.User))!;
        if (list.SellerId != user.Id) return Results.Forbid();
        
        var img = list.GalleryImages.FirstOrDefault(i => i.Id == imageId);
        if (img is null) return Results.NotFound();
        if (list.GalleryImages.Count <= 1) return Results.Problem("Minst en bild behövs finnas", statusCode: 400);

        var key = img.StorageKey;
        list.GalleryImages.Remove(img);
        await db.SaveChangesAsync();
        try
        {
            await pictureStorage.DeleteAsync(key);
        }
        catch
        {
            //if deleting did not work then logg it all we can do and manually delete?
        }
        return Results.Ok(ToDetail(list, user.UserName ?? "unknown"));
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

