using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Orders;

public class GetMyDownloads : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me/downloads", Handler)
            .RequireAuthorization()
            .WithName("GetMyDownloads")
            .Produces<IReadOnlyList<OrderContracts.DownloadSummary>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handler(
        HttpContext context,
        UserManager<User> users,
        SharePrintDbContext db,
        CancellationToken ct)
    {
        var user = await users.GetUserAsync(context.User);
        if (user is null) return Results.Unauthorized();

        var downloads = await db.OrderItems
            .Where(i => i.DownloadPath && i.Grant != null)
            .Join(db.Orders,
                i => i.OrderId,
                o => o.Id,
                (i, o) => new { i, o })
            .Where(x => x.o.BuyerId == user.Id && x.o.Status == OrderStatus.Paid)
            .Join(db.Listings,
                x => x.i.ListingId,
                l => l.Id,
                (x, l) => new { x.i, x.o, l })
            .OrderByDescending(x => x.o.CreatedAt)
            .Select(x => new OrderContracts.DownloadSummary(
                x.o.Id,
                x.i.Id,
                x.l.Title,
                x.o.CreatedAt,
                x.i.Grant!.DownloadRemaining))
            .ToListAsync(ct);

        return Results.Ok(downloads);
    }
}
