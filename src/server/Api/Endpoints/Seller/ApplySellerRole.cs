using Microsoft.AspNetCore.Identity;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;

namespace SharePrint.Api.Endpoints.Seller;

/// <summary>
/// Two-step seller onboarding:
///   1) POST /api/seller/apply/setup-intent  -> create Stripe SetupIntent, return clientSecret
///   2) POST /api/seller/apply/confirm       -> verify SetupIntent succeeded, grant Seller role
/// </summary>
public class ApplySellerRole : IEndpoint
{
    public record ConfirmRequest(string SetupIntentId);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/seller/apply/setup-intent",
            async (HttpContext ctx, UserManager<User> users, ISellerOnboarding onboarding, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(ctx.User);
            if (user is null) return Results.Unauthorized();
            if (await users.IsInRoleAsync(user, Roles.Seller))
                return Results.Problem("Har redan behörigheten för att skapa produkter.");

            var clientSecret = await onboarding.CreateCardSetupAsync(user.Id, ct);
            return Results.Ok(new { clientSecret });
        }).RequireAuthorization();

        app.MapPost("/api/seller/apply/confirm",
            async (HttpContext ctx, ConfirmRequest req, UserManager<User> users, ISellerOnboarding onboarding, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(ctx.User);
            if (user is null) return Results.Unauthorized();

            var ok = await onboarding.ConfirmCardAndPromoteAsync(user.Id, req.SetupIntentId, ct);
            return ok
                ? Results.Ok()
                : Results.Problem("Kortverifiering misslyckades. Försök igen.");
        }).RequireAuthorization();
    }
}
