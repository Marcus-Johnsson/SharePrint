using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Market;

public class PatchListing : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/listings/{id}", Handler)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("PatchListing")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private record Request(string Title, string Description, decimal Price);

    private static async Task<IResult> Handler(
        [FromRoute] Guid id,
        Request req,
        HttpContext context,
        UserManager<User> users,
        SharePrintDbContext db)
    {
        var listing = await db.Listings.FindAsync(id);
        if (listing is null) return TypedResults.NotFound();

        var user = (await users.GetUserAsync(context.User))!;
        if (listing.SellerId != user.Id) return TypedResults.Forbid();

        listing.Title       = req.Title;
        listing.Description = req.Description;
        listing.Price       = req.Price;

        await db.SaveChangesAsync();
        return TypedResults.Ok();
    }
}
