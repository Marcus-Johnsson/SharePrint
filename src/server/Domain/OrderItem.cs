namespace SharePrint.Domain;

public enum OrderItemType { Download }

public class OrderItem
{
    public Guid Id { get; set; } =  Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ListingId { get; set; }
    public OrderItemType Type { get; set; } = OrderItemType.Download;
    public decimal UnitPrice { get; set; }
    public DownloadGrant? grant { get; set; }
}