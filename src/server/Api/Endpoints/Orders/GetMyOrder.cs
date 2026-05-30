using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Orders;

public class GetMyOrder : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/{id:guid}", Handler)
            .RequireAuthorization()
            .WithName("GetMyOrder")
            .Produces<OrderContracts.OrderDetail>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handler(
        Guid id,
        HttpContext context,
        UserManager<User> users,
        SharePrintDbContext db,
        CancellationToken ct)
    {
        var user = await users.GetUserAsync(context.User);
        if (user is null) return Results.Unauthorized();

        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Grant)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return Results.NotFound();
        if (order.BuyerId != user.Id) return Results.Forbid();

        var listingIds = order.Items.Select(i => i.ListingId).Distinct().ToList();
        var titles = await db.Listings
            .Where(l => listingIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Title, ct);

        var dto = new OrderContracts.OrderDetail(
            order.Id,
            order.Status.ToString(),
            order.TotalPrice,
            order.Currency,
            order.CreatedAt,
            order.Items.Select(i => new OrderContracts.OrderItem(
                i.Id,
                i.ListingId,
                titles.GetValueOrDefault(i.ListingId, "(deleted)"),
                i.UnitPrice,
                i.DownloadPath,
                i.PrintPath,
                i.Grant?.DownloadRemaining
            )).ToList()
        );

        return Results.Ok(dto);
    }
}
