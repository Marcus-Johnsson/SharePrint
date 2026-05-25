using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Market;

public class GetProductDetails : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/listings/{id}", Handler)
            .RequireAuthorization()
            .WithName("GetProductDetails");
    }

    private static async Task<IResult> Handler(
            [FromRoute] Guid id,
            SharePrintDbContext db,
            UserManager<User> user)
    {
        var info = await db.Listings.Include(p => p.GalleryImages)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (info is null || info.Status != ListingStatus.Active) return Results.NotFound();
        var seller = await user.FindByIdAsync(info.SellerId);
        return Results.Ok(ToDetail(info, seller?.UserName ?? "unknown"));
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

