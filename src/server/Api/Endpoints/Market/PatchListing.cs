using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SharePrint.Api.Contracts;
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
            .WithName("PatchListing");
    }

    private async static Task<IResult> Handler(
        [FromRoute] Guid id,
        ListingContracts.UpdateListingRequest request,
        HttpContext context,
        UserManager<User> users,
        SharePrintDbContext db)
    {
        var list = await db.Listings.FindAsync(id);
        if (list == null) return Results.NotFound();
        var user = await users.GetUserAsync(context.User);
        if (list.SellerId != user.Id) return Results.Forbid();
        
        list.Title = request.Title;
        list.Description = request.Description;
        list.Price = request.Price;

        await db.SaveChangesAsync();
        return Results.Ok();
    }
}