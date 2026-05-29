using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Checkout;

/// <summary>
/// POST /api/checkout/create-intent
///
/// Persists a Pending Order with one OrderItem per cart entry, then creates a Stripe
/// PaymentIntent whose metadata carries the orderId. Confirm endpoint flips status to Paid
/// and issues DownloadGrants after Stripe reports success.
/// </summary>
public class CreatePaymentIntent : IEndpoint
{
    public record CartItemDto(Guid Id, string? Option); // Option: "print" | "download" | null
    public record Request(List<CartItemDto> Items);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/checkout/create-intent",
            async (Request req,
                   HttpContext ctx,
                   UserManager<User> users,
                   SharePrintDbContext db,
                   ICheckoutPayments payments,
                   IConfiguration cfg,
                   CancellationToken ct) =>
            {
                var user = await users.GetUserAsync(ctx.User);
                if (user is null) return Results.Unauthorized();

                if (req.Items is null || req.Items.Count == 0)
                    return Results.Problem("Cart is empty.", statusCode: 400);

                var ids = req.Items.Select(i => i.Id).Distinct().ToList();
                var listings = await db.Listings
                    .Where(l => ids.Contains(l.Id) && l.Status == ListingStatus.Active)
                    .ToDictionaryAsync(l => l.Id, ct);

                if (listings.Count != ids.Count)
                    return Results.Problem("One or more listings unavailable.", statusCode: 400);

                var printSurcharge = decimal.TryParse(cfg["Checkout:PrintSurcharge"], out var s) ? s : 0m;

                var order = new Order
                {
                    BuyerId = user.Id,
                    Currency = "SEK",
                    Status = OrderStatus.Pending
                };

                decimal totalSek = 0m;
                string currency = "sek";

                foreach (var item in req.Items)
                {
                    var l = listings[item.Id];
                    currency = l.Currency.ToLowerInvariant();

                    var unit = l.Price;
                    var download = false;
                    var print = false;

                    if (item.Option == "print")
                    {
                        if (!l.PrintAble)
                            return Results.Problem($"Listing {l.Id} is not printable.", statusCode: 400);
                        unit += printSurcharge;
                        print = true;
                    }
                    else if (item.Option == "download")
                    {
                        if (!l.DownloadAble)
                            return Results.Problem($"Listing {l.Id} is not downloadable.", statusCode: 400);
                        download = true;
                    }
                    else
                    {
                        return Results.Problem($"Listing {l.Id}: option must be 'print' or 'download'.", statusCode: 400);
                    }

                    order.Items.Add(new OrderItem
                    {
                        ListingId = l.Id,
                        UnitPrice = unit,
                        DownloadPath = download,
                        PrintPath = print
                    });

                    totalSek += unit;
                }

                if (totalSek <= 0) return Results.Problem("Total must be > 0.", statusCode: 400);

                order.TotalPrice = totalSek;
                order.Currency = currency.ToUpperInvariant();

                db.Orders.Add(order);
                await db.SaveChangesAsync(ct);

                var amountMinor = (long)Math.Round(totalSek * 100m, MidpointRounding.AwayFromZero);

                var pi = await payments.CreateIntentAsync(
                    amountMinor,
                    currency,
                    user.Id,
                    metadata: new Dictionary<string, string> { ["orderId"] = order.Id.ToString() },
                    ct);

                order.StripePaymentIntentId = pi.PaymentIntentId;
                await db.SaveChangesAsync(ct);

                return Results.Ok(new { clientSecret = pi.ClientSecret, orderId = order.Id });
            }).RequireAuthorization();
    }
}
