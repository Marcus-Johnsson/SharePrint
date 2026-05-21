using SharePrint.Application.Abstractions;

namespace SharePrint.Infrastructure.Storage;

// Path for CloudStorage later on
public class CloudPictureStorage : IPictureStorage
{
    public Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct = default)
        => throw new NotImplementedException("v1");

    public Task<StoredPicture> OpenReadAsync(string storageKey, CancellationToken ct = default)
        => throw new NotImplementedException("v1");

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
        => throw new NotImplementedException("v1");
}