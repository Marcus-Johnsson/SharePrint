using SharePrint.Application.Abstractions;

namespace SharePrint.Infrastructure.Storage;

public class LocalDiskPictureStorage : IPictureStorage
{
    private static readonly HashSet<string> AllowedMime = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private readonly string _root;
    private readonly string _metaDir;

    public LocalDiskPictureStorage(string rootPath)
    {
        _root = Path.GetFullPath(rootPath);
        _metaDir = Path.Combine(_root, "meta");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_metaDir);
    }

    public async Task<string> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        if (!AllowedMime.Contains(contentType))
            throw new InvalidDataException($"Bild format ej tillåtet: {contentType}");

        var key = Guid.NewGuid().ToString("N");
        await using (var save = File.Create(Path.Combine(_root, key)))
            await content.CopyToAsync(save, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_metaDir, key), contentType, cancellationToken);
        return key;
    }

    public async Task<StoredPicture> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var info = Path.Combine(_root, storageKey);
        var meta = Path.Combine(_metaDir, storageKey);

        if (!File.Exists(info) || !File.Exists(meta))
            throw new FileNotFoundException(storageKey);
        var fileTrim = (await File.ReadAllTextAsync(meta, cancellationToken)).Trim();
        return new StoredPicture(File.OpenRead(info), fileTrim);
    }
    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var blob = Path.Combine(_root, storageKey);
        var meta = Path.Combine(_metaDir, storageKey);
        if (File.Exists(blob)) File.Delete(blob);
        if (File.Exists(meta)) File.Delete(meta);
        return Task.CompletedTask;
    }
}