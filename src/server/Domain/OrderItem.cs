using System.ComponentModel.DataAnnotations;

namespace SharePrint.Domain;

public enum OrderItemType { Download }

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public Guid ListingId { get; set; }

    [EnumDataType(typeof(OrderItemType))]
    public OrderItemType Type { get; set; } = OrderItemType.Download;

    [Range(typeof(decimal), "1", "1000")]
    public decimal UnitPrice { get; set; }

    public DownloadGrant? Grant { get; set; }
}
