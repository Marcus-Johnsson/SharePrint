using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Listings;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Market;

public class GetUserListings : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/listings/user/{page}/{pageSize}", Handler)
            .WithName("GetUserCatalog")
            .Produces<IReadOnlyList<ListingContracts.ListingSummary>>(StatusCodes.Status200OK);
    }
    public record ListingPage(
        IReadOnlyList<ListingContracts.ListingSummary> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);
    
    private static async Task<IResult> Handler(
        SharePrintDbContext db,
        UserManager<User> users,
        HttpContext context,
        [FromRoute] int page = 1,
        [FromRoute] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var user = await users.GetUserAsync(context.User);
        if (user is null) return Results.Unauthorized();    

        var listingCount = db.Listings.Where(l => l.Status == ListingStatus.Active && l.SellerId == user.Id);
        var totalCount = await listingCount.CountAsync();
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        
        var items = await db.Listings
            .Where(l => l.Status == ListingStatus.Active && l.SellerId == user.Id)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<ListingContracts.ListingSummary>(items.Count);
        foreach (var listing in items)
        {
            var seller = await users.FindByIdAsync(listing.SellerId);
            result.Add(new ListingContracts.ListingSummary(
                listing.Id,
                listing.Title,
                DescriptionPreview.From(listing.Description),
                listing.Price,
                $"/api/pictures/{listing.MarketPictureKey}",
                seller?.UserName ?? "unknown",
                listing.DownloadAble,
                listing.PrintAble));
        }

        return TypedResults.Ok(new ListingPage(
            result, page, pageSize, totalCount, totalPages,
            page < totalPages, page > 1));
    }
}
