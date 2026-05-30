using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.Endpoints.Checkout;


public class CreatePaymentIntent : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/checkout/create-intent", Handler)
            .RequireAuthorization()
            .WithName("CreatePaymentIntent")
            .Produces<CheckoutContracts.CreateIntentResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private record CartItem(Guid Id, string? Option); // Option: "print" | "download"
    private record Request(List<CartItem> Items);

    private static async Task<IResult> Handler(
        Request req,
        HttpContext context,
        UserManager<User> users,
        SharePrintDbContext db,
        ICheckoutPayments payments,
        IConfiguration cfg,
        CancellationToken ct)
    {
        var user = await users.GetUserAsync(context.User);
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

        return Results.Ok(new CheckoutContracts.CreateIntentResult(pi.ClientSecret, order.Id));
    }
}
