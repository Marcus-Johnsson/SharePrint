using System.ComponentModel.DataAnnotations;

namespace SharePrint.Domain;

public enum OrderStatus { Pending, Paid }

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string BuyerId { get; set; } = "";

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal TotalPrice { get; set; } = 0;

    [Required]
    public string Currency { get; set; } = "SEK";

    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [StringLength(255)]
    public string? StripePaymentIntentId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [MinLength(1)]
    public List<OrderItem> Items { get; set; } = new();
}
