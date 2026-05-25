using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Market;

public class PostChangeListingStatus : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/listings/{id}/status", Handler)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("UnlistListing");
    }

    private static async Task<IResult> Handler(
        [FromRoute] Guid id,
        HttpContext context,
        UserManager<User> users,
        SharePrintDbContext db)
    {
        var listing = await db.Listings.FindAsync(id);
        if (listing == null) return Results.NotFound();
        var user = await users.GetUserAsync(context.User);
        if (user.Id != listing.SellerId) return Results.Forbid();

        listing.Status = listing.Status == ListingStatus.Active
            ? ListingStatus.Unlisted
            : ListingStatus.Active;
       
        await db.SaveChangesAsync();
        return Results.Ok();
    }
}