using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Orders;

public class ConfirmOrder : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/confirm", Handler)
            .RequireAuthorization()
            .WithName("ConfirmOrder")
            .Produces<OrderContracts.ConfirmResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }

    private record Request(string PaymentIntentId);

    private static async Task<IResult> Handler(
        Request req,
        HttpContext context,
        UserManager<User> users,
        SharePrintDbContext db,
        ICheckoutPayments payments,
        ILogger<ConfirmOrder> logger,
        CancellationToken ct)
    {
        var user = await users.GetUserAsync(context.User);
        if (user is null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(req.PaymentIntentId))
            return Results.Problem("paymentIntentId required.", statusCode: 400);

        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Grant)
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == req.PaymentIntentId, ct);

        if (order is null) return Results.NotFound();
        if (order.BuyerId != user.Id) return Results.Forbid();

        if (order.Status == OrderStatus.Paid)
            return Results.Ok(new OrderContracts.ConfirmResult(order.Id, "Paid"));

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

        return Results.Ok(new OrderContracts.ConfirmResult(order.Id, "Paid"));
    }
}
