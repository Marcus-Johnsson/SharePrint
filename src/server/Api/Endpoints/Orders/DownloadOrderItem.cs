using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Orders;

public class DownloadOrderItem : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/{orderId:guid}/items/{itemId:guid}/download", Handler)
            .RequireAuthorization()
            .WithName("DownloadOrderItem")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status410Gone);
    }

    private static async Task<IResult> Handler(
        Guid orderId,
        Guid itemId,
        HttpContext context,
        UserManager<User> users,
        SharePrintDbContext db,
        IFileStorage files,
        CancellationToken ct)
    {
        var user = await users.GetUserAsync(context.User);
        if (user is null) return Results.Unauthorized();

        var item = await db.OrderItems
            .Include(i => i.Grant)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.OrderId == orderId, ct);
        if (item is null) return Results.NotFound();

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return Results.NotFound();
        if (order.BuyerId != user.Id) return Results.Forbid();
        if (order.Status != OrderStatus.Paid)
            return Results.Problem("Order not paid.", statusCode: 409);

        if (!item.DownloadPath || item.Grant is null)
            return Results.Problem("Item is not downloadable.", statusCode: 400);
        if (item.Grant.DownloadRemaining <= 0)
            return Results.Problem("Download limit reached.", statusCode: 410);

        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == item.ListingId, ct);
        if (listing is null) return Results.NotFound();

        item.Grant.IssueDownload();
        await db.SaveChangesAsync(ct);

        var stored = await files.OpenReadAsync(listing.StorageKey, ct);
        return Results.File(
            stored.Content,
            stored.ContentType,
            fileDownloadName: stored.OriginalFileName);
    }
}
