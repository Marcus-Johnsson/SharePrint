using System.ComponentModel.DataAnnotations;

namespace SharePrint.Domain;

public class ListingImage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ListingId { get; set; }

    [Required]
    [StringLength(500)]
    public string StorageKey { get; set; } = "";

    public int Order { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}