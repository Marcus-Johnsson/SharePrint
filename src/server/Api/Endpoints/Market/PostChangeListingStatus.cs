using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Market;

public class PostUnlistListing : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/seller/unlist/{id}", Handler)
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
        listing.Status = ListingStatus.Unlisted;
        await db.SaveChangesAsync();
        return Results.Ok();
    }
}