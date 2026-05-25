using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;

namespace SharePrint.Api.IntegrationTests;

public class ListingImageEntityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public ListingImageEntityTests(WebApplicationFactory<Program> factory) => _factory = factory;

    internal static byte[] JpegBytes(int size = 32)
    {
        var b = new byte[size];
        b[0] = 0xFF; b[1] = 0xD8; b[2] = 0xFF; b[3] = 0xE0;
        return b;
    }

    internal static byte[] PngBytes(int size = 32)
    {
        var b = new byte[size];
        b[0] = 0x89; b[1] = 0x50; b[2] = 0x4E; b[3] = 0x47;
        b[4] = 0x0D; b[5] = 0x0A; b[6] = 0x1A; b[7] = 0x0A;
        return b;
    }

    internal async Task<HttpClient> Seller()
    {
        var client = _factory.CreateClient();
        var email = $"s{Guid.NewGuid():N}@test.com";
        var password = "Passw0rd!1";
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            username = $"u{Guid.NewGuid():N}"
        });
        await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        await client.PostAsync("/api/seller/apply", null);
        return client;
    }

    internal static MultipartFormDataContent BuildCreateForm(
        string title = "Cool File",
        string description = "desc",
        string price = "49.00",
        bool downloadAble = true,
        bool printAble = false,
        byte[]? file = null,
        byte[]? thumb = null,
        IReadOnlyList<byte[]>? gallery = null)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(title), "title");
        form.Add(new StringContent(description), "description");
        form.Add(new StringContent(price), "price");
        form.Add(new StringContent(downloadAble.ToString()), "downloadAble");
        form.Add(new StringContent(printAble.ToString()), "printAble");

        var f = new ByteArrayContent(file ?? Encoding.UTF8.GetBytes("FILEDATA"));
        f.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(f, "file", "product.bin");

        if (thumb is not null)
        {
            var t = new ByteArrayContent(thumb);
            t.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(t, "thumbnail", "thumb.jpg");
        }
        gallery ??= new[] { PngBytes() };
        foreach (var g in gallery)
        {
            var gc = new ByteArrayContent(g);
            gc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            form.Add(gc, "galleryImages", "g.png");
        }
        return form;
    }
    
    [Fact]
    public async Task Create_with_thumb_and_gallery_returns_picture_urls()
    {
        var client = await Seller();
        using var form = BuildCreateForm(thumb: JpegBytes(), gallery: new[] { PngBytes(), PngBytes() });
        var res = await client.PostAsync("/api/listings", form);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("/api/pictures/", body);
        Assert.DoesNotContain("MarketPictureKey", body);
        Assert.DoesNotContain("StorageKey", body);
    }
}