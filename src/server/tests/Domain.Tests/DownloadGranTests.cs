namespace SharePrint.Domain.Tests;

public class DownloadGranTests
{
    [Fact]
    public void IssueDownload_decrements_and_stamps()
    {
        var g = new DownloadGrant { OrderItemId = Guid.NewGuid(), DownloadRemaining = 5 };
        g.IssueDownload();
        Assert.Equal(4, g.DownloadRemaining);
        Assert.NotNull(g.LastDownloadedAt);
    }

    [Fact]
    public void IssueDownload_throws_when_exausted()
    {
        var g = new DownloadGrant { OrderItemId = Guid.NewGuid(), DownloadRemaining = 0 };
        Assert.Throws<InvalidOperationException>(() => g.IssueDownload());
    }
}