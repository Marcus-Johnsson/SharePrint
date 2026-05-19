using SharePrint.Application.Abstractions;

namespace SharePrint.Infrastructure.Storage;

public class LocalDiskStorage : IFileStorage
{
    private readonly string _root;
    private readonly string _metaDir;

    public LocalDiskStorage(string rootPath)
    {
        _root = Path.GetFullPath(rootPath);
        _metaDir = Path.Combine(_root, "meta");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_metaDir);
    }

    public async Task<string> SaveAsync(Stream content, string contentType, string originalFileName, CancellationToken ct = default)
    {
        var key = Guid.NewGuid().ToString("N");
        await using (var fs = File.Create(Path.Combine(_root, key)))
            await content.CopyToAsync(fs, ct);
        await File.WriteAllLinesAsync(Path.Combine(_metaDir, key), new[] { contentType, originalFileName }, ct);
        return key;
    }

    public async Task<StoredFile> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var path = Path.Combine(_root, storageKey);
        if ( !File.Exists(path)) throw new FileNotFoundException(storageKey);
        var meta = await File.ReadAllLinesAsync(Path.Combine(_metaDir, storageKey), ct);
        return new StoredFile(File.OpenRead(path), meta[0], meta[1]);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        File.Delete(Path.Combine(_metaDir, storageKey));
        File.Delete(Path.Combine(_metaDir, storageKey));
        return Task.CompletedTask;
    }
}