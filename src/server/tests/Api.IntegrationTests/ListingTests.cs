using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SharePrint.Api.IntegrationTests;

public class ListingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public ListingTests(WebApplicationFactory<Program> factory) =>  _factory = factory;

    private async Task<HttpClient> Seller()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new
            {
                email = $"T_{Guid.NewGuid():N}@test.com", password = "PasswOrd!1", username = $"T_{Guid.NewGuid():N}"
            });
        await client.PostAsync("/api/seller/apply", null);
        return client;
    }
    
    [Fact(Skip = "Replaced by ListingPicturesTests")]
    public async Task Create_lists_in_catalog_without_exposing_key()
    {
        var client = await Seller();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Cool File"), "title");
        form.Add(new StringContent("desc"), "description");
        form.Add(new StringContent("49.00"), "price");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("FILEDATA")), "file", "original-secret.txt");
        
        var create = await client.PostAsync("/api/auth/listings", form);
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        
        var catalog = await client.GetStringAsync("/api/listings");
        Assert.Contains("Cool File", catalog);
        Assert.DoesNotContain("original-secret", catalog);
        Assert.DoesNotContain("StorageKey", catalog);
    }

    [Fact (Skip = "Replaced by ListingPicturesTests")]
    public async Task Customer_cannot_create_listing()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new
            {
                email = $"{Guid.NewGuid():N}@test.com",
                password = "PasswOrd!",
                username = "T_{Guid.NewGuid():N}"
            });
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Cool File"), "title");
        form.Add(new StringContent("desc"), "description");
        form.Add(new StringContent("49.00"), "price");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("FILEDATA")), "file", "original-secret.txt");
        var res = await client.PostAsync("/api/listings", form);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}