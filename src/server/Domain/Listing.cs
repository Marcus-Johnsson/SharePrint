namespace SharePrint.Domain;
public enum ListingStatus { Active, Unlisted }

public class Listing
{
    public Guid Id { get; set;} = Guid.NewGuid();
    public string SellerId { get; set;} = "";
    public string Title { get; set;} = "";
    public string Description { get; set;} = "";
    public decimal Price { get; set;}
    public string Currency { get; set;} = "SEK";
    public string StorageKey { get; set;} = "";
    public string OriginalFileName { get; set;} = "";
    public string ContentType { get; set;} = "";
    public long SizeBytes { get; set;}
    public ListingStatus Status { get; set; } = ListingStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
}