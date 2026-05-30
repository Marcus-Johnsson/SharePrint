using Microsoft.AspNetCore.Identity;
using SharePrint.Api.Contracts;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;

namespace SharePrint.Api.Endpoints.Seller;

public class CreateSellerSetupIntent : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/seller/apply/setup-intent", Handler)
            .RequireAuthorization()
            .WithName("CreateSellerSetupIntent")
            .Produces<SellerContracts.SetupIntentResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handler(
        HttpContext context,
        UserManager<User> users,
        ISellerOnboarding onboarding,
        CancellationToken ct)
    {
        var user = await users.GetUserAsync(context.User);
        if (user is null) return Results.Unauthorized();
        if (await users.IsInRoleAsync(user, Roles.Seller))
            return Results.Problem("Har redan behörigheten för att skapa produkter.", statusCode: 400);

        var clientSecret = await onboarding.CreateCardSetupAsync(user.Id, ct);
        return Results.Ok(new SellerContracts.SetupIntentResult(clientSecret));
    }
}
