using System.ComponentModel.DataAnnotations;

namespace SharePrint.Domain;

public class DownloadGrant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OrderItemId { get; set; }

    [Range(0, 5)]
    public int DownloadRemaining { get; set; } = 5;

    public DateTimeOffset LastDownloadedAt { get; set; }

    public void IssueDownload()
    {
        if (DownloadRemaining <= 0)
        {
            throw new InvalidOperationException("Download grant exhausted");
        }
        DownloadRemaining--;
        LastDownloadedAt = DateTimeOffset.UtcNow;
    }
}
