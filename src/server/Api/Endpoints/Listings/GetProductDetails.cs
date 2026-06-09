using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Listings;

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
        UserManager<User> users,
        HttpContext context)
    {
        var listing = await db.Listings
            .Include(l => l.GalleryImages)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null)
            return TypedResults.NotFound();

        var user = await users.GetUserAsync(context.User);
        var isOwner = user is not null && listing.SellerId == user.Id;
        if (!isOwner && listing.Status == ListingStatus.Unlisted)
            return TypedResults.NotFound();

        return TypedResults.Ok(PostCreateListing.ToDetail(listing, user?.UserName ?? "unknown"));
    }
}
