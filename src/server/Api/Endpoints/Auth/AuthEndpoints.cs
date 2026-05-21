using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SharePrint.Api.Endpoints._internal;
using SharePrint.Domain;
using static SharePrint.Api.Contracts.AuthContracts;

namespace SharePrint.Api.Endpoints.Auth;

public class AuthEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var action = app.MapGroup("/api/auth");
        action.MapPost("/register",
            async (RegisterRequest RegisterRequest, UserManager<User> users) =>
            {
                var newUser = new User
                {
                    UserName = RegisterRequest.Username, Email = RegisterRequest.Email
                };
                var result = await users.CreateAsync(newUser, RegisterRequest.Password);
                if (!result.Succeeded)
                    return Results.Problem(string.Join("; ", result.Errors.Select(e => e.Description)),
                        statusCode: 400);
                await users.AddToRoleAsync(newUser, "customer");
                return Results.Ok(new { userId = newUser.Id, displayName = newUser.UserName });
            });

        action.MapPost("/login",
            async (LoginRequest loginRequest, UserManager<User> users, SignInManager<User> signin) =>
            {
                var user = await users.FindByEmailAsync(loginRequest.Email);
                if (user is null) return Results.Problem("Invalid login attempt", statusCode: 401);
                var result = await signin.PasswordSignInAsync(user, loginRequest.Password, true, false);
                return result.Succeeded ? Results.Ok() : Results.Problem("Invalid login attempt", statusCode: 401);
            });

        action.MapPost("/logout",
            async (SignInManager<User> signin) =>
            {
                await signin.SignOutAsync();
                return Results.Ok();
            }).RequireAuthorization();

        action.MapPost("/me",
            async (UserManager<User> users, HttpContext context) =>
            {
                var user = await users.GetUserAsync(context.User);
                if (user == null) return Results.Unauthorized();

                var roles = await users.GetRolesAsync(user);
                return Results.Ok(new MeResponse(user.Id, user.Email, user.UserName, roles.ToArray()));
            });

    }
}