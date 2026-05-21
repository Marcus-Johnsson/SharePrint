using Microsoft.AspNetCore.Identity;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;

namespace SharePrint.Api.Endpoints.Seller;

public class ApplySellerRole : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/seller/apply", async (HttpContext context, UserManager<User> users) =>
        {
            var user = await users.GetUserAsync(context.User);
            if (user is null) return Results.Unauthorized();
            if(await users.IsInRoleAsync(user, Roles.Seller))
                return Results.Problem("Already a seller");
            if (!await users.IsInRoleAsync(user, Roles.Seller))
                await users.AddToRoleAsync(user, Roles.Seller);
            return Results.Ok();
        }).RequireAuthorization();
    }
}