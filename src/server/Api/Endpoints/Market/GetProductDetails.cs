using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Api.Endpoints.Seller;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Market;

public class GetProductDetails : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/listings/{id}", Handler)
            .WithName("GetProductDetails")
            .Produces<ListingContracts.ListingDetail>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handler(
        [FromRoute] Guid id,
        SharePrintDbContext db,
        UserManager<User> users)
    {
        var listing = await db.Listings
            .Include(l => l.GalleryImages)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null || listing.Status != ListingStatus.Active)
            return TypedResults.NotFound();

        var seller = await users.FindByIdAsync(listing.SellerId);
        return TypedResults.Ok(ListingEndpoints.ToDetail(listing, seller?.UserName ?? "unknown"));
    }
}
