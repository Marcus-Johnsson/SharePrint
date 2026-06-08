using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SharePrint.Api.IntegrationTests;

public class SellerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public SellerTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact (Skip = "No longer correct since strip was implemented")]
    public async Task Add_seller_role()
    {
        var (client, _) = await CreateFakeAuth.RegisterAndLoginAsync(_factory);

        var apply = await client.PostAsync("/api/seller/apply", null);

        Assert.Equal(HttpStatusCode.OK, apply.StatusCode);

        var meRes = await client.GetAsync("/api/auth/me");
        meRes.EnsureSuccessStatusCode();
        var me = await meRes.Content.ReadFromJsonAsync<AuthTests.MeDto>();
        Assert.Contains("Seller", me!.roles);
    }
}
