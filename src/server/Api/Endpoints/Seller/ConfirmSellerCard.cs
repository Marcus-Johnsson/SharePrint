using Microsoft.AspNetCore.Identity;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;

namespace SharePrint.Api.Endpoints.Seller;

public class ConfirmSellerCard : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/seller/apply/confirm", Handler)
            .RequireAuthorization()
            .WithName("ConfirmSellerCard")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private record Request(string SetupIntentId);

    private static async Task<IResult> Handler(
        Request req,
        HttpContext context,
        UserManager<User> users,
        ISellerOnboarding onboarding,
        CancellationToken ct)
    {
        var user = await users.GetUserAsync(context.User);
        if (user is null) return Results.Unauthorized();

        var ok = await onboarding.ConfirmCardAndPromoteAsync(user.Id, req.SetupIntentId, ct);
        return ok
            ? Results.Ok()
            : Results.Problem("Kortverifiering misslyckades. Försök igen.", statusCode: 400);
    }
}
