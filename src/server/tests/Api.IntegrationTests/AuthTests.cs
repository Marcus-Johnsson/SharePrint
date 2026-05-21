using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SharePrint.Api.IntegrationTests;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public AuthTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Register_then_me_returns_user_with_customer_role()
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new
            {
                email = $"{Guid.NewGuid():N}@test.com",
                password = "Passw0rd!",
                username = $"TestCustomer{Guid.NewGuid():N}"
            });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var me = await client.GetFromJsonAsync<MeDto>("/api/auth/me");

        Assert.Contains("customer", me!.roles);
    }
    public record MeDto(string id, string email, string displayName, string[] roles);

}