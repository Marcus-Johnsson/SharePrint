namespace SharePrint.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string contentType, string originalFileName, CancellationToken cancellationToken = default);
    Task<StoredFile> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}