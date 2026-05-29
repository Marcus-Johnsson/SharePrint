using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Orders;

/// <summary>
/// POST /api/orders/confirm
/// Body: { paymentIntentId }
///
/// Re-fetches the PaymentIntent from Stripe, verifies status=succeeded, flips Order.Status
/// to Paid, and issues DownloadGrants for OrderItems with DownloadPath=true. Idempotent.
/// </summary>
public class ConfirmOrder : IEndpoint
{
    public record Request(string PaymentIntentId);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/confirm",
            async (Request req,
                   HttpContext ctx,
                   UserManager<User> users,
                   SharePrintDbContext db,
                   ICheckoutPayments payments,
                   ILogger<ConfirmOrder> logger,
                   CancellationToken ct) =>
            {
                var user = await users.GetUserAsync(ctx.User);
                if (user is null) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(req.PaymentIntentId))
                    return Results.Problem("paymentIntentId required.", statusCode: 400);

                var order = await db.Orders
                    .Include(o => o.Items).ThenInclude(i => i.Grant)
                    .FirstOrDefaultAsync(o => o.StripePaymentIntentId == req.PaymentIntentId, ct);

                if (order is null) return Results.NotFound();
                if (order.BuyerId != user.Id) return Results.Forbid();

                if (order.Status == OrderStatus.Paid)
                {
                    // Already confirmed (idempotent).
                    return Results.Ok(new { orderId = order.Id, status = "Paid" });
                }

                var succeeded = await payments.IsSucceededAsync(req.PaymentIntentId, ct);
                if (!succeeded)
                    return Results.Problem("Payment not yet succeeded.", statusCode: 409);

                order.Status = OrderStatus.Paid;

                foreach (var item in order.Items)
                {
                    if (item.DownloadPath && item.Grant is null)
                    {
                        db.DownloadGrants.Add(new DownloadGrant
                        {
                            OrderItemId = item.Id,
                            DownloadRemaining = 5,
                            LastDownloadedAt = DateTimeOffset.UtcNow
                        });
                    }
                }

                await db.SaveChangesAsync(ct);
                logger.LogInformation("Order {OrderId} confirmed (PI {Pi}) for {Buyer}",
                    order.Id, req.PaymentIntentId, user.Id);

                return Results.Ok(new { orderId = order.Id, status = "Paid" });
            }).RequireAuthorization();
    }
}
