using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SharePrint.Api.IntegrationTests;

/// <summary>
/// Helpers for authenticating an HttpClient against the cookie-based Identity auth.
/// The API uses IdentityConstants.ApplicationScheme (cookie) — not JWT — so the
/// "token" is really an auth cookie attached to the HttpClient's cookie container.
/// </summary>
public static class CreateFakeAuth
{
    public record TestUser(string Email, string Password, string Username);

    public static TestUser NewUser() => new(
        Email:    $"u{Guid.NewGuid():N}@test.se",
        Password: "Passw0rd!",
        Username: $"User{Guid.NewGuid():N}");
    
    public static async Task<(HttpClient client, TestUser user)> RegisterAndLoginAsync(
        WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var user = NewUser();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { email = user.Email, password = user.Password, username = user.Username });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = user.Email, password = user.Password });
        login.EnsureSuccessStatusCode();

        return (client, user);
    }
}
