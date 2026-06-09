using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Listings;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Listings;

public class GetCatalog : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/listings/{page}/{pageSize}/{filter}", Handler)
            .WithName("GetCatalog")
            .Produces<IReadOnlyList<ListingContracts.ListingSummary>>(StatusCodes.Status200OK);
    }
    public record ListingPage(
        IReadOnlyList<ListingContracts.ListingSummary> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage,
        ListingFilter Filter);

    public enum ListingFilter { None, Withdrawal, Download }
    
    private static async Task<IResult> Handler(
        SharePrintDbContext db,
        UserManager<User> users,
        [FromRoute] int page = 1,
        [FromRoute] int pageSize = 5,
        [FromRoute] ListingFilter filter = ListingFilter.None)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = db.Listings.Where(l => l.Status == ListingStatus.Active);

        q = filter switch
        {
            ListingFilter.Withdrawal => q.Where(l => l.PrintAble),
            ListingFilter.Download    => q.Where(l => l.DownloadAble),
            _                      => q
        };

        var totalCount = await q.CountAsync();
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await q
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var sellerIds = items.Select(i => i.SellerId).Distinct().ToList();
        var sellers = await users.Users
            .Where(u => sellerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "unknown");

        var result = items.Select(listing => new ListingContracts.ListingSummary(
            listing.Id,
            listing.Title,
            DescriptionPreview.From(listing.Description),
            listing.Price,
            $"/api/pictures/{listing.MarketPictureKey}",
            sellers.GetValueOrDefault(listing.SellerId, "unknown"),
            listing.DownloadAble,
            listing.PrintAble)).ToList();

        return TypedResults.Ok(new ListingPage(
            result, page, pageSize, totalCount, totalPages,
            page < totalPages, page > 1, filter));
    }
}
