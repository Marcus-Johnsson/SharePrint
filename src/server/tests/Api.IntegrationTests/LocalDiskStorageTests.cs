using SharePrint.Application.Abstractions;
using SharePrint.Infrastructure.Storage;

namespace SharePrint.Api.IntegrationTests;

public class LocalDiskStorageTests : FileStorageTests
{
    protected override IFileStorage CreateStorage()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fmtest-" + Guid.NewGuid());
        return new LocalDiskStorage(dir);
    }
}