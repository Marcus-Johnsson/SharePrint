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
            .WithName("ChangeListingStatus")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private record Request(string Status);

    private static async Task<IResult> Handler(
        [FromRoute] Guid id,
        Request req,
        HttpContext context,
        UserManager<User> users,
        SharePrintDbContext db)
    {
        if (!Enum.TryParse<ListingStatus>(req.Status, ignoreCase: true, out var newStatus))
            return TypedResults.Problem($"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<ListingStatus>())}",
                statusCode: 400);

        var listing = await db.Listings.FindAsync(id);
        if (listing is null) return TypedResults.NotFound();

        var user = await users.GetUserAsync(context.User);
        if (listing.SellerId != user.Id) return TypedResults.Forbid();

        listing.Status = newStatus;
        await db.SaveChangesAsync();
        return TypedResults.Ok();
    }
}
