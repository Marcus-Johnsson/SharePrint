using System.ComponentModel.DataAnnotations;

namespace SharePrint.Domain;

public enum ListingStatus { Active, Unlisted }

public class Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string SellerId { get; set; } = "";

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Title { get; set; } = "";

    [StringLength(2000)]
    public string Description { get; set; } = "";

    [Range(typeof(decimal), "1", "1000000")]
    public decimal Price { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be ISO 4217 (3 uppercase letters).")]
    public string Currency { get; set; } = "SEK";

    [Required]
    [StringLength(500)]
    public string StorageKey { get; set; } = "";

    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = "";

    [Required]
    [StringLength(100)]
    [RegularExpression(@"^[\w.+-]+/[\w.+-]+$", ErrorMessage = "ContentType must be a valid MIME type.")]
    public string ContentType { get; set; } = "";

    [Range(1, 524_288_000)]
    public long SizeBytes { get; set; }

    [EnumDataType(typeof(ListingStatus))]
    public ListingStatus Status { get; set; } = ListingStatus.Active;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
