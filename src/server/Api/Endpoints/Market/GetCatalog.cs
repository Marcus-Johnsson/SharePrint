using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Listings;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;
using static SharePrint.Api.Contracts.ListingContracts;

namespace SharePrint.Api.Endpoints.Market;

public class ListingContract : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/seller/catalog", Handler)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("CreateListing");
    }
    private static async Task<IResult> Handler(
        SharePrintDbContext db,
        UserManager<User> users,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if(pageSize is < 1 or > 100) pageSize = 20;

        var items = await db.Listings.Where(l => l.Status == ListingStatus.Active)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<ListingSummary>(items.Count);
        foreach (var listing in items)
        {
            var seller = await users.FindByIdAsync(listing.SellerId);
            result.Add(ToSummary(listing, seller?.UserName ?? "unknown"));
        }
        return Results.Ok(result);
    }
    internal static ListingSummary ToSummary(Listing l, string sellerUsername) =>  // These can be turned into a dto to prevent copies
        new(
            l.Id,
            l.Title,
            DescriptionPreview.From(l.Description),
            l.Price,
            $"/api/pictures/{l.MarketPictureKey}",
            sellerUsername);
}

