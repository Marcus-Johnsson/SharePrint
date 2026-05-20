using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SharePrint.Api.IntegrationTests;

public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public HealthTests(WebApplicationFactory<Program> factory) => _factory = factory;
    
    [Fact]
    public async Task Health_returns_ok()
    {
        var result = await _factory.CreateClient().GetAsync("api/health");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains("ok", await result.Content.ReadAsStringAsync());
    }
}