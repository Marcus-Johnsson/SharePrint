namespace SharePrint.Application.Abstractions;

public interface IPictureStorage
{
    Task<string> SaveAsync(Stream content, string contentType,
        CancellationToken cancellationToken = default);
    Task<StoredPicture> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public record StoredPicture(Stream Content, string ContentType);