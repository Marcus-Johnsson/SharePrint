using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Orders;

/// <summary>
/// GET /api/orders/{id}
/// Returns order + items + grant info for the authenticated buyer.
/// </summary>
public class GetMyOrder : IEndpoint
{
    public record OrderItemDto(
        Guid Id,
        Guid ListingId,
        string ListingTitle,
        decimal UnitPrice,
        bool DownloadPath,
        bool PrintPath,
        int? DownloadsRemaining);

    public record OrderDto(
        Guid Id,
        string Status,
        decimal TotalPrice,
        string Currency,
        DateTimeOffset CreatedAt,
        List<OrderItemDto> Items);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/{id:guid}",
            async (Guid id,
                   HttpContext ctx,
                   UserManager<User> users,
                   SharePrintDbContext db,
                   CancellationToken ct) =>
            {
                var user = await users.GetUserAsync(ctx.User);
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

                var dto = new OrderDto(
                    order.Id,
                    order.Status.ToString(),
                    order.TotalPrice,
                    order.Currency,
                    order.CreatedAt,
                    order.Items.Select(i => new OrderItemDto(
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
            }).RequireAuthorization();
    }
}
