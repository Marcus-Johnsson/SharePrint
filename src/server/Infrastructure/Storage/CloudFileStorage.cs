using SharePrint.Application.Abstractions;

namespace SharePrint.Infrastructure.Storage;


// Path for CloudStorage later on
public class CloudFileStorage : IFileStorage
{
    public Task<string> SaveAsync(Stream content, string contentType, string originalFileName, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("v1");

    public Task<StoredFile> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("v1");

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("v1");
}