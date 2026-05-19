namespace SharePrint.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string contentType, string originalFileName, CancellationToken cy = default);
    Task<StoredFile> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}