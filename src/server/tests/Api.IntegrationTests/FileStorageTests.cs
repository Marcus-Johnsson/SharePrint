using System.Text;
using SharePrint.Application.Abstractions;

namespace SharePrint.Api.IntegrationTests;

public abstract class FileStorageTests
{
    protected abstract IFileStorage CreateStorage();

    [Fact]
    public async Task Save_Then_OpenRead_round_trips()
    {
        // save file
        var s = CreateStorage();
        var key = await s.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes("hello")), "text/plain", "a.txt");
        // find
        var f = await s.OpenReadAsync(key);
        // read file
        using var r = new StreamReader(f.Content);
        Assert.Equal("hello", r.ReadToEnd());
        Assert.Equal("text/plain", f.ContentType);
    }
    
    [Fact]
    public async Task StorageKey_does_not_contain_original_fileName()
    {
        var s = CreateStorage();
        var key = await s.SaveAsync(new MemoryStream(new byte[] { 1 }), "application/octet-stream", "secret-name.psd");
        Assert.DoesNotContain("secret-name.psd", key);
    }
}